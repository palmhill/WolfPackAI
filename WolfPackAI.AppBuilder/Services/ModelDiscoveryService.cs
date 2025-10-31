using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using WolfPackAI.AppBuilder.Configuration;

namespace WolfPackAI.AppBuilder.Services;

public static class ModelDiscoveryService
{
    public static async Task AugmentWithCloudModelsAsync(LiteLLMConfiguration configuration, ProviderApiKeys? providerApiKeys, CancellationToken cancellationToken = default)
    {
        if (configuration == null)
        {
            return;
        }
        if (providerApiKeys == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(providerApiKeys.OpenAI))
        {
            await AddOpenAiModelsAsync(configuration, providerApiKeys.OpenAI, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(providerApiKeys.Anthropic))
        {
            await AddAnthropicModelsAsync(configuration, providerApiKeys.Anthropic, cancellationToken);
        }
    }

    private static async Task AddOpenAiModelsAsync(LiteLLMConfiguration configuration, string apiKey, CancellationToken cancellationToken)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await http.GetAsync("https://api.openai.com/v1/models", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"OpenAI model list request failed: {(int)response.StatusCode} {response.ReasonPhrase} - {body}");
            return;
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var modelsResponse = await JsonSerializer.DeserializeAsync<OpenAiModelsResponse>(stream, options, cancellationToken);
        if (modelsResponse?.Data == null)
        {
            return;
        }

        var existingNames = new HashSet<string>(configuration.ModelList.Select(m => m.ModelName));
        foreach (var model in modelsResponse.Data)
        {
            if (string.IsNullOrWhiteSpace(model.Id))
            {
                continue;
            }

            var modelName = $"openai/{model.Id}";
            if (existingNames.Contains(modelName))
            {
                continue;
            }

            var isEmbedding = IsOpenAiEmbeddingModel(model.Id);
            var supportsReasoning = !isEmbedding && IsOpenAiReasoningModel(model.Id);
            configuration.ModelList.Add(new ModelConfig
            {
                ModelName = modelName,
                LiteLLMParams = new LiteLLMParams
                {
                    Model = model.Id,
                    ApiKey = "OPENAI_API_KEY",
                    SupportsReasoning = supportsReasoning,
                    Mode = isEmbedding ? "embedding" : string.Empty
                },
                ModelType = "openai"
            });
            existingNames.Add(modelName);
        }
    }

    private static async Task AddAnthropicModelsAsync(LiteLLMConfiguration configuration, string apiKey, CancellationToken cancellationToken)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("x-api-key", apiKey);
        http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        using var response = await http.GetAsync("https://api.anthropic.com/v1/models", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"Anthropic model list request failed: {(int)response.StatusCode} {response.ReasonPhrase} - {body}");
            return;
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var modelsResponse = await JsonSerializer.DeserializeAsync<AnthropicModelsResponse>(stream, options, cancellationToken);
        if (modelsResponse?.Data == null)
        {
            return;
        }

        var existingNames = new HashSet<string>(configuration.ModelList.Select(m => m.ModelName));
        foreach (var model in modelsResponse.Data)
        {
            if (string.IsNullOrWhiteSpace(model.Id))
            {
                continue;
            }
            var modelName = $"anthropic/{model.Id}";
            if (existingNames.Contains(modelName))
            {
                continue;
            }

            configuration.ModelList.Add(new ModelConfig
            {
                ModelName = modelName,
                LiteLLMParams = new LiteLLMParams
                {
                    Model = model.Id,
                    ApiKey = "ANTHROPIC_API_KEY",
                    SupportsReasoning = IsAnthropicReasoningModel(model.Id)
                },
                ModelType = "anthropic"
            });
            existingNames.Add(modelName);
        }
    }

    private static bool IsOpenAiEmbeddingModel(string modelId)
    {
        if (modelId.IndexOf("embedding", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        // Known embeddings (extend as needed)
        return modelId.Equals("text-embedding-3-small", StringComparison.OrdinalIgnoreCase)
            || modelId.Equals("text-embedding-3-large", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOpenAiReasoningModel(string modelId)
    {
               
        var isReasoning = !string.IsNullOrWhiteSpace(modelId) && (modelId.StartsWith("gpt-5") || modelId.StartsWith("gpt-4o") || modelId.StartsWith("o4") || modelId.StartsWith("o3"));


        return isReasoning;
    }

    private static bool IsAnthropicReasoningModel(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return false;
        }

        var id = modelId.Trim().ToLowerInvariant();

        // Anthropic exposes "thinking" on Claude 3.7 Sonnet and potential future variants
        if (id.Contains("claude-3-7") && id.Contains("sonnet"))
        {
            return true;
        }

        // Be permissive if Anthropic adds explicit reasoning/thinking suffixes
        if (id.Contains("thinking") || id.Contains("reasoning"))
        {
            return true;
        }

        return false;
    }

    private sealed class OpenAiModelsResponse
    {
        public List<OpenAiModel> Data { get; set; } = new();
    }

    private sealed class OpenAiModel
    {
        public string Id { get; set; } = string.Empty;
    }

    private sealed class AnthropicModelsResponse
    {
        public List<AnthropicModel> Data { get; set; } = new();
    }

    private sealed class AnthropicModel
    {
        public string Id { get; set; } = string.Empty;
    }
}


