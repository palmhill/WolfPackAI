using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WolfPackAI.PrimeGate.Configuration;
using WolfPackAI.PrimeGate.Models.Responses;

namespace WolfPackAI.PrimeGate.Services;

/// <summary>
/// Auto-discovery service with memory caching and parallel discovery
/// </summary>
public class AutoDiscoveryService : IAutoDiscoveryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AutoDiscoveryService> _logger;
    private readonly EndpointsConfiguration _endpoints;
    private readonly ModelDiscoveryConfiguration _discoveryConfig;
    private readonly PrimeGateSettings _primeGateSettings;
    private readonly List<ModelConfiguration> _litellmModels;
    private readonly List<ModelConfiguration> _cursorModels;

    private const string CacheKey = "llm_inventory";

    public AutoDiscoveryService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<AutoDiscoveryService> logger,
        IOptions<EndpointsConfiguration> endpoints,
        IOptions<ModelDiscoveryConfiguration> discoveryConfig,
        IOptions<PrimeGateSettings> primeGateSettings,
        IOptions<List<ModelConfiguration>> litellmModels,
        IOptions<List<ModelConfiguration>> cursorModels)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
        _endpoints = endpoints.Value;
        _discoveryConfig = discoveryConfig.Value;
        _primeGateSettings = primeGateSettings.Value;
        _litellmModels = litellmModels.Value;
        _cursorModels = cursorModels.Value;
    }

    /// <inheritdoc/>
    public async Task<LLMInventoryResponse> DiscoverLLMsAsync()
    {
        // Try to get from cache
        if (_cache.TryGetValue(CacheKey, out LLMInventoryResponse? cachedInventory) && cachedInventory != null)
        {
            _logger.LogDebug("Returning cached LLM inventory with {Count} models", cachedInventory.TotalModels);
            return cachedInventory;
        }

        // Discover and cache
        var inventory = await DiscoverLLMsInternalAsync();

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_primeGateSettings.DiscoveryCacheSeconds)
        };

        _cache.Set(CacheKey, inventory, cacheOptions);
        _logger.LogInformation("Discovered and cached {Count} models", inventory.TotalModels);

        return inventory;
    }

    /// <inheritdoc/>
    public async Task RefreshCacheAsync()
    {
        _cache.Remove(CacheKey);
        await DiscoverLLMsAsync();
        _logger.LogInformation("Cache refreshed");
    }

    private async Task<LLMInventoryResponse> DiscoverLLMsInternalAsync()
    {
        var models = new List<LLMModel>();

        if (_discoveryConfig.ParallelDiscovery)
        {
            // Parallel discovery for better performance
            var tasks = new List<Task<List<LLMModel>>>();

            if (_discoveryConfig.EnableOllama)
                tasks.Add(DiscoverOllamaAsync());

            if (_discoveryConfig.EnableLiteLLM)
                tasks.Add(DiscoverLiteLLMAsync());

            if (_discoveryConfig.EnableCursorNative)
                tasks.Add(Task.FromResult(GetCursorNativeModels()));

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                models.AddRange(result);
            }
        }
        else
        {
            // Sequential discovery
            if (_discoveryConfig.EnableOllama)
                models.AddRange(await DiscoverOllamaAsync());

            if (_discoveryConfig.EnableLiteLLM)
                models.AddRange(await DiscoverLiteLLMAsync());

            if (_discoveryConfig.EnableCursorNative)
                models.AddRange(GetCursorNativeModels());
        }

        return new LLMInventoryResponse
        {
            TotalModels = models.Count,
            Models = models,
            LastUpdate = DateTime.UtcNow,
            Status = models.Any() ? "available" : "no-models"
        };
    }

    private async Task<List<LLMModel>> DiscoverOllamaAsync()
    {
        var models = new List<LLMModel>();

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(_primeGateSettings.HealthCheckTimeoutSeconds);
            client.BaseAddress = new Uri(_endpoints.Ollama.BaseUrl);

            var response = await client.GetAsync(_endpoints.Ollama.TagsEndpoint);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<OllamaTagsResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data?.Models != null)
                {
                    foreach (var model in data.Models)
                    {
                        models.Add(new LLMModel
                        {
                            Name = model.Name,
                            Source = "ollama",
                            Status = "online",
                            Type = "local",
                            Capabilities = new[] { "code", "chat", "local", "fast" },
                            SizeGB = model.Size / 1_000_000_000.0
                        });
                    }

                    _logger.LogInformation("Discovered {Count} Ollama models", models.Count);
                }
            }
            else
            {
                _logger.LogWarning("Ollama health check failed with status {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // Graceful degradation - log but don't throw
            _logger.LogWarning(ex, "Failed to discover Ollama models - continuing with graceful degradation");
        }

        return models;
    }

    private async Task<List<LLMModel>> DiscoverLiteLLMAsync()
    {
        var models = new List<LLMModel>();

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(_primeGateSettings.HealthCheckTimeoutSeconds);
            client.BaseAddress = new Uri(_endpoints.LiteLLM.BaseUrl);

            var response = await client.GetAsync(_endpoints.LiteLLM.HealthEndpoint);

            if (response.IsSuccessStatusCode)
            {
                // LiteLLM is online - add configured cloud models
                foreach (var modelConfig in _litellmModels)
                {
                    models.Add(new LLMModel
                    {
                        Name = modelConfig.Name,
                        Source = "litellm",
                        Status = "online",
                        Type = modelConfig.Type,
                        Capabilities = modelConfig.Capabilities,
                        Cost = modelConfig.CostPer1KTokens
                    });
                }

                _logger.LogInformation("Discovered {Count} LiteLLM models", models.Count);
            }
            else
            {
                _logger.LogWarning("LiteLLM health check failed with status {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // Graceful degradation - log but don't throw
            _logger.LogWarning(ex, "Failed to discover LiteLLM models - continuing with graceful degradation");
        }

        return models;
    }

    private List<LLMModel> GetCursorNativeModels()
    {
        var models = new List<LLMModel>();

        try
        {
            foreach (var modelConfig in _cursorModels)
            {
                models.Add(new LLMModel
                {
                    Name = modelConfig.Name,
                    Source = "cursor-native",
                    Status = "online",
                    Type = modelConfig.Type,
                    Capabilities = modelConfig.Capabilities
                });
            }

            _logger.LogInformation("Added {Count} Cursor native models", models.Count);
        }
        catch (Exception ex)
        {
            // Should never happen, but graceful degradation
            _logger.LogWarning(ex, "Failed to get Cursor native models");
        }

        return models;
    }
}
