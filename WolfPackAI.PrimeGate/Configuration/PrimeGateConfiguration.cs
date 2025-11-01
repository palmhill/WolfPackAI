namespace WolfPackAI.PrimeGate.Configuration;

/// <summary>
/// Main PrimeGate service configuration
/// </summary>
public class PrimeGateSettings
{
    public string ServiceName { get; set; } = "WolfPackAI.PrimeGate";
    public string Version { get; set; } = "1.0.0";
    public string Role { get; set; } = "compliant-expose-all";
    public string OrchestratorAuthority { get; set; } = "full";
    public int Port { get; set; } = 7000;
    public int DiscoveryCacheSeconds { get; set; } = 30;
    public int HealthCheckTimeoutSeconds { get; set; } = 3;
    public bool EnableSwagger { get; set; } = true;
    public bool EnableCors { get; set; } = true;
    public string[] CorsOrigins { get; set; } = Array.Empty<string>();
}

/// <summary>
/// External service endpoint configuration
/// </summary>
public class EndpointsConfiguration
{
    public LiteLLMEndpoint LiteLLM { get; set; } = new();
    public OllamaEndpoint Ollama { get; set; } = new();
}

/// <summary>
/// LiteLLM service endpoint configuration
/// </summary>
public class LiteLLMEndpoint
{
    public string BaseUrl { get; set; } = "http://litellm:4000";
    public string HealthEndpoint { get; set; } = "/health";
    public string ChatCompletionsEndpoint { get; set; } = "/v1/chat/completions";
    public string ModelsEndpoint { get; set; } = "/v1/models";
    public int Timeout { get; set; } = 30;
}

/// <summary>
/// Ollama service endpoint configuration
/// </summary>
public class OllamaEndpoint
{
    public string BaseUrl { get; set; } = "http://ollama:11434";
    public string TagsEndpoint { get; set; } = "/api/tags";
    public string GenerateEndpoint { get; set; } = "/api/generate";
    public string HealthEndpoint { get; set; } = "/";
    public int Timeout { get; set; } = 30;
}

/// <summary>
/// Model discovery configuration
/// </summary>
public class ModelDiscoveryConfiguration
{
    public bool EnableOllama { get; set; } = true;
    public bool EnableLiteLLM { get; set; } = true;
    public bool EnableCursorNative { get; set; } = true;
    public bool ParallelDiscovery { get; set; } = true;
    public string FailureMode { get; set; } = "graceful";
}

/// <summary>
/// Access policy configuration
/// </summary>
public class PolicyConfiguration
{
    public int TokenLimit { get; set; } = int.MaxValue;
    public int RequestsPerMinute { get; set; } = int.MaxValue;
    public int ConcurrentModels { get; set; } = int.MaxValue;
    public double CostLimit { get; set; } = double.MaxValue;
    public List<string> Restrictions { get; set; } = new();
}

/// <summary>
/// Model suggestion configuration
/// </summary>
public class ModelSuggestionConfiguration
{
    public bool EnableIntelligentSuggestions { get; set; } = true;
    public double DefaultConfidence { get; set; } = 0.5;
    public Dictionary<string, string> PreferredModels { get; set; } = new();
}

/// <summary>
/// Model configuration for LiteLLM models
/// </summary>
public class ModelConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string[] Capabilities { get; set; } = Array.Empty<string>();
    public double? CostPer1KTokens { get; set; }
}
