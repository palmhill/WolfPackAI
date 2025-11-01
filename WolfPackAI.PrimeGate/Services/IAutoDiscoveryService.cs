using WolfPackAI.PrimeGate.Models.Responses;

namespace WolfPackAI.PrimeGate.Services;

/// <summary>
/// Service for auto-discovering LLM models from various sources
/// </summary>
public interface IAutoDiscoveryService
{
    /// <summary>
    /// Discover all available LLM models from configured sources
    /// </summary>
    /// <returns>Complete inventory of discovered models</returns>
    Task<LLMInventoryResponse> DiscoverLLMsAsync();

    /// <summary>
    /// Force refresh the discovery cache
    /// </summary>
    Task RefreshCacheAsync();
}
