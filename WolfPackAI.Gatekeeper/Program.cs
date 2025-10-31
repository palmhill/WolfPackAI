using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddHttpClient();
builder.Services.AddSingleton<AutoDiscoveryService>();

var app = builder.Build();

// Enable Swagger for API documentation
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Auto-discovery service
var discovery = app.Services.GetRequiredService<AutoDiscoveryService>();

// API Endpoints

// GET /api/llms - List all available LLMs from all sources
app.MapGet("/api/llms", async () =>
{
    var inventory = await discovery.DiscoverLLMs();
    return Results.Json(inventory);
})
.WithName("GetLLMs")
.WithOpenApi();

// GET /api/status - Gatekeeper status
app.MapGet("/api/status", () =>
{
    return Results.Json(new
    {
        service = "WolfPackAI.Gatekeeper",
        version = "1.0.0",
        status = "online",
        role = "compliant-expose-all",
        orchestrator_authority = "full",
        timestamp = DateTime.UtcNow
    });
})
.WithName("GetStatus")
.WithOpenApi();

// POST /api/suggest - Suggest best LLM for a task (orchestrator decides)
app.MapPost("/api/suggest", async ([FromBody] TaskRequest request) =>
{
    var inventory = await discovery.DiscoverLLMs();
    var suggestion = SuggestBestModel(request.Task, request.Context, inventory);

    return Results.Json(new
    {
        suggestion = suggestion,
        note = "This is a suggestion only. Orchestrator has final decision.",
        all_available = inventory.Models.Select(m => m.Name).ToList()
    });
})
.WithName("SuggestModel")
.WithOpenApi();

// POST /api/execute - Execute task with selected LLM(s)
app.MapPost("/api/execute", async ([FromBody] ExecutionRequest request, HttpClient httpClient) =>
{
    try
    {
        var result = await ExecuteWithLLM(request, httpClient);
        return Results.Json(result);
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            status = "error",
            message = ex.Message,
            note = "Gatekeeper reports errors but lets orchestrator handle them"
        });
    }
})
.WithName("ExecuteTask")
.WithOpenApi();

// GET /api/policy - Access policy (unlimited)
app.MapGet("/api/policy", () =>
{
    return Results.Json(new
    {
        token_limit = int.MaxValue,
        requests_per_minute = int.MaxValue,
        concurrent_models = int.MaxValue,
        cost_limit = double.MaxValue,
        restrictions = new List<string>(),
        note = "Orchestrator has unlimited access to all resources"
    });
})
.WithName("GetPolicy")
.WithOpenApi();

Console.WriteLine("🐺 WolfPackAI Gatekeeper starting...");
Console.WriteLine("   Role: Compliant LLM Exposure");
Console.WriteLine("   Authority: Orchestrator (Full Control)");
Console.WriteLine("   Mode: Seamless Injection");
Console.WriteLine($"   Listening on: http://localhost:7000");
Console.WriteLine();

app.Run("http://localhost:7000");

// Helper method: Suggest best model
static ModelSuggestion SuggestBestModel(string task, string? context, LLMInventory inventory)
{
    var taskLower = task.ToLowerInvariant();

    // Simple heuristics (orchestrator can override)
    if (taskLower.Contains("refactor") || taskLower.Contains("code review"))
        return FindModel(inventory, "claude-sonnet", "Best for code structure");

    if (taskLower.Contains("algorithm") || taskLower.Contains("complex logic"))
        return FindModel(inventory, "gpt-4", "Best for complex logic");

    if (taskLower.Contains("fast") || taskLower.Contains("quick"))
        return FindModel(inventory, "deepseek-coder", "Fastest local model");

    if (taskLower.Contains("documentation") || taskLower.Contains("comment"))
        return FindModel(inventory, "gpt-3.5", "Cost-effective for docs");

    if (taskLower.Contains("security") || taskLower.Contains("audit"))
        return FindModel(inventory, "claude-opus", "Most thorough");

    // Default: first available model
    var firstModel = inventory.Models.FirstOrDefault();
    return new ModelSuggestion
    {
        ModelName = firstModel?.Name ?? "cursor-fast",
        Reason = "Default suggestion",
        Confidence = 0.5
    };
}

static ModelSuggestion FindModel(LLMInventory inventory, string preferredName, string reason)
{
    var model = inventory.Models.FirstOrDefault(m =>
        m.Name.Contains(preferredName, StringComparison.OrdinalIgnoreCase));

    return new ModelSuggestion
    {
        ModelName = model?.Name ?? inventory.Models.FirstOrDefault()?.Name ?? "cursor-fast",
        Reason = model != null ? reason : "Preferred model not available, using fallback",
        Confidence = model != null ? 0.9 : 0.5
    };
}

// Helper method: Execute with LLM
static async Task<object> ExecuteWithLLM(ExecutionRequest request, HttpClient httpClient)
{
    // Route to appropriate endpoint based on model
    var endpoint = GetEndpointForModel(request.SelectedModel);

    var response = await httpClient.PostAsJsonAsync(endpoint, new
    {
        model = request.SelectedModel,
        prompt = request.Prompt,
        options = request.Options
    });

    var content = await response.Content.ReadAsStringAsync();

    return new
    {
        status = response.IsSuccessStatusCode ? "success" : "error",
        model_used = request.SelectedModel,
        response = content,
        orchestrator_request_id = request.RequestId
    };
}

static string GetEndpointForModel(string modelName)
{
    if (modelName.Contains("deepseek") || modelName.Contains("qwen") ||
        modelName.Contains("llama") || modelName.Contains("codellama"))
        return "http://localhost:1143/api/generate";

    if (modelName.Contains("gpt") || modelName.Contains("claude") ||
        modelName.Contains("gemini") || modelName.Contains("mistral"))
        return "http://localhost:4000/v1/chat/completions";

    // Default to LiteLLM gateway
    return "http://localhost:4000/v1/chat/completions";
}

// Models
public record TaskRequest(string Task, string? Context);
public record ExecutionRequest(string RequestId, string SelectedModel, string Prompt, Dictionary<string, object>? Options);
public record ModelSuggestion
{
    public string ModelName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

// Auto-discovery service
public class AutoDiscoveryService
{
    private readonly HttpClient _httpClient;

    public AutoDiscoveryService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(3);
    }

    public async Task<LLMInventory> DiscoverLLMs()
    {
        var models = new List<LLMModel>();

        // Try to discover Ollama models
        await TryDiscoverOllama(models);

        // Try to discover LiteLLM models
        await TryDiscoverLiteLLM(models);

        // Add Cursor native models (always available)
        models.Add(new LLMModel
        {
            Name = "cursor-fast",
            Source = "cursor-native",
            Status = "online",
            Type = "embedded",
            Capabilities = new[] { "code", "chat", "fast" }
        });

        models.Add(new LLMModel
        {
            Name = "cursor-smart",
            Source = "cursor-native",
            Status = "online",
            Type = "embedded",
            Capabilities = new[] { "code", "chat", "quality" }
        });

        return new LLMInventory
        {
            TotalModels = models.Count,
            Models = models,
            LastUpdate = DateTime.UtcNow,
            Status = models.Any() ? "available" : "no-models"
        };
    }

    private async Task TryDiscoverOllama(List<LLMModel> models)
    {
        try
        {
            var response = await _httpClient.GetAsync("http://localhost:1143/api/tags");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<OllamaTagsResponse>(content);

                if (data?.models != null)
                {
                    foreach (var model in data.models)
                    {
                        models.Add(new LLMModel
                        {
                            Name = model.name,
                            Source = "ollama",
                            Status = "online",
                            Type = "local",
                            Capabilities = new[] { "code", "chat", "local", "fast" },
                            SizeGB = model.size / 1_000_000_000.0
                        });
                    }
                }
            }
        }
        catch
        {
            // Silent fail - Ollama not running
        }
    }

    private async Task TryDiscoverLiteLLM(List<LLMModel> models)
    {
        try
        {
            // LiteLLM typical cloud models
            var litellmModels = new[]
            {
                new LLMModel { Name = "gpt-4-turbo", Source = "litellm", Type = "cloud", Capabilities = new[] { "code", "chat", "complex", "quality" }, Cost = 0.01 },
                new LLMModel { Name = "gpt-4", Source = "litellm", Type = "cloud", Capabilities = new[] { "code", "chat", "quality" }, Cost = 0.03 },
                new LLMModel { Name = "gpt-3.5-turbo", Source = "litellm", Type = "cloud", Capabilities = new[] { "code", "chat", "fast", "cheap" }, Cost = 0.001 },
                new LLMModel { Name = "claude-opus-4", Source = "litellm", Type = "cloud", Capabilities = new[] { "code", "chat", "quality", "thorough" }, Cost = 0.015 },
                new LLMModel { Name = "claude-sonnet-4", Source = "litellm", Type = "cloud", Capabilities = new[] { "code", "chat", "balanced" }, Cost = 0.003 },
                new LLMModel { Name = "claude-haiku-4", Source = "litellm", Type = "cloud", Capabilities = new[] { "code", "chat", "fast", "cheap" }, Cost = 0.00025 },
                new LLMModel { Name = "gemini-pro", Source = "litellm", Type = "cloud", Capabilities = new[] { "code", "chat", "cheap" }, Cost = 0.0005 }
            };

            var response = await _httpClient.GetAsync("http://localhost:4000/health");
            if (response.IsSuccessStatusCode)
            {
                models.AddRange(litellmModels.Select(m => m with { Status = "online" }));
            }
        }
        catch
        {
            // Silent fail - LiteLLM not running
        }
    }
}

// Response models
public record OllamaTagsResponse(OllamaModel[]? models);
public record OllamaModel(string name, long size, string modified_at);

public record LLMInventory
{
    public int TotalModels { get; set; }
    public List<LLMModel> Models { get; set; } = new();
    public DateTime LastUpdate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public record LLMModel
{
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = "unknown";
    public string Type { get; set; } = string.Empty;
    public string[] Capabilities { get; set; } = Array.Empty<string>();
    public double? SizeGB { get; set; }
    public double? Cost { get; set; } // Cost per 1K tokens
}
