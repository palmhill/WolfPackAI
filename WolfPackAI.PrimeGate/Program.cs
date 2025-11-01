using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WolfPackAI.PrimeGate.Configuration;
using WolfPackAI.PrimeGate.Models.Requests;
using WolfPackAI.PrimeGate.Models.Responses;
using WolfPackAI.PrimeGate.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure settings from appsettings.json
builder.Services.Configure<PrimeGateSettings>(
    builder.Configuration.GetSection("PrimeGate"));
builder.Services.Configure<EndpointsConfiguration>(
    builder.Configuration.GetSection("Endpoints"));
builder.Services.Configure<ModelDiscoveryConfiguration>(
    builder.Configuration.GetSection("ModelDiscovery"));
builder.Services.Configure<PolicyConfiguration>(
    builder.Configuration.GetSection("Policy"));
builder.Services.Configure<ModelSuggestionConfiguration>(
    builder.Configuration.GetSection("ModelSuggestion"));
builder.Services.Configure<List<ModelConfiguration>>(
    builder.Configuration.GetSection("LiteLLMModels"));
builder.Services.Configure<List<ModelConfiguration>>(
    "CursorNativeModels",
    builder.Configuration.GetSection("CursorNativeModels"));

// Add services to DI container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "WolfPackAI PrimeGate API",
        Version = "v1.0.0",
        Description = "Compliant LLM exposure service for Cursor Orchestrator integration"
    });
});

// Configure CORS
var primeGateSettings = builder.Configuration.GetSection("PrimeGate").Get<PrimeGateSettings>()
    ?? new PrimeGateSettings();

if (primeGateSettings.EnableCors)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var origins = primeGateSettings.CorsOrigins;
            if (origins.Contains("*"))
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                policy.WithOrigins(origins)
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
        });
    });
}

// Add HTTP client with resilience policies
builder.Services.AddHttpClient();

// Add memory cache for discovery service
builder.Services.AddMemoryCache();

// Register application services
builder.Services.AddSingleton<IAutoDiscoveryService, AutoDiscoveryService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
    var logger = sp.GetRequiredService<ILogger<AutoDiscoveryService>>();
    var endpoints = sp.GetRequiredService<IOptions<EndpointsConfiguration>>();
    var discoveryConfig = sp.GetRequiredService<IOptions<ModelDiscoveryConfiguration>>();
    var settings = sp.GetRequiredService<IOptions<PrimeGateSettings>>();
    var litellmModels = sp.GetRequiredService<IOptions<List<ModelConfiguration>>>();

    // Get cursor models from named options
    var cursorModels = Options.Create(
        builder.Configuration.GetSection("CursorNativeModels").Get<List<ModelConfiguration>>()
        ?? new List<ModelConfiguration>());

    return new AutoDiscoveryService(
        httpClientFactory,
        cache,
        logger,
        endpoints,
        discoveryConfig,
        settings,
        litellmModels,
        cursorModels);
});

builder.Services.AddScoped<IModelSuggestionService, ModelSuggestionService>();
builder.Services.AddScoped<IExecutionService, ExecutionService>();

var app = builder.Build();

// Enable Swagger for API documentation
if (primeGateSettings.EnableSwagger || app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "WolfPackAI PrimeGate API v1");
        options.RoutePrefix = "swagger";
    });
}

// Enable CORS
if (primeGateSettings.EnableCors)
{
    app.UseCors();
}

// API Endpoints

/// <summary>
/// GET /api/status - PrimeGate service status
/// Returns current status and role information
/// </summary>
app.MapGet("/api/status", (IOptions<PrimeGateSettings> settings) =>
{
    var config = settings.Value;
    var response = new StatusResponse
    {
        Service = config.ServiceName,
        Version = config.Version,
        Status = "online",
        Role = config.Role,
        OrchestratorAuthority = config.OrchestratorAuthority,
        Timestamp = DateTime.UtcNow
    };

    return Results.Ok(response);
})
.WithName("GetStatus")
.WithOpenApi()
.Produces<StatusResponse>(200);

/// <summary>
/// GET /api/llms - List all available LLMs from all sources
/// Returns comprehensive inventory of discovered models
/// </summary>
app.MapGet("/api/llms", async (IAutoDiscoveryService discoveryService) =>
{
    var inventory = await discoveryService.DiscoverLLMsAsync();
    return Results.Ok(inventory);
})
.WithName("GetLLMs")
.WithOpenApi()
.Produces<LLMInventoryResponse>(200);

/// <summary>
/// POST /api/suggest - Suggest best LLM for a task
/// Orchestrator has final decision authority
/// </summary>
app.MapPost("/api/suggest", async (
    [FromBody] TaskRequest request,
    IAutoDiscoveryService discoveryService,
    IModelSuggestionService suggestionService) =>
{
    var inventory = await discoveryService.DiscoverLLMsAsync();
    var suggestion = await suggestionService.SuggestModelAsync(request, inventory);

    return Results.Ok(suggestion);
})
.WithName("SuggestModel")
.WithOpenApi()
.Produces<SuggestionResponse>(200);

/// <summary>
/// POST /api/execute - Execute task with selected LLM(s)
/// Executes the task and returns results or error information
/// </summary>
app.MapPost("/api/execute", async (
    [FromBody] ExecutionRequest request,
    IExecutionService executionService) =>
{
    var result = await executionService.ExecuteAsync(request);

    // Always return 200 OK with status in body for graceful degradation
    return Results.Ok(result);
})
.WithName("ExecuteTask")
.WithOpenApi()
.Produces<ExecutionResponse>(200);

/// <summary>
/// GET /api/policy - Access policy information
/// Returns unlimited access policy for orchestrator
/// </summary>
app.MapGet("/api/policy", (IOptions<PolicyConfiguration> policyConfig) =>
{
    var config = policyConfig.Value;
    var response = new PolicyResponse
    {
        TokenLimit = config.TokenLimit,
        RequestsPerMinute = config.RequestsPerMinute,
        ConcurrentModels = config.ConcurrentModels,
        CostLimit = config.CostLimit,
        Restrictions = config.Restrictions,
        Note = "Orchestrator has unlimited access to all resources"
    };

    return Results.Ok(response);
})
.WithName("GetPolicy")
.WithOpenApi()
.Produces<PolicyResponse>(200);

// Display startup information
Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine("  WolfPackAI PrimeGate");
Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine($"  Service:    {primeGateSettings.ServiceName}");
Console.WriteLine($"  Version:    {primeGateSettings.Version}");
Console.WriteLine($"  Role:       {primeGateSettings.Role}");
Console.WriteLine($"  Authority:  {primeGateSettings.OrchestratorAuthority}");
Console.WriteLine($"  Port:       {primeGateSettings.Port}");
Console.WriteLine($"  Swagger:    {(primeGateSettings.EnableSwagger ? "Enabled" : "Disabled")}");
Console.WriteLine($"  CORS:       {(primeGateSettings.EnableCors ? "Enabled" : "Disabled")}");
Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine($"  Listening:  http://localhost:{primeGateSettings.Port}");
Console.WriteLine($"  Swagger:    http://localhost:{primeGateSettings.Port}/swagger");
Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine();
Console.WriteLine("  Press Ctrl+C to shutdown");
Console.WriteLine();

// Run the application
app.Run($"http://localhost:{primeGateSettings.Port}");
