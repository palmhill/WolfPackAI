namespace WolfPackAI.PrimeGate.Models.Responses;

/// <summary>
/// Response for /api/llms endpoint containing all discovered models
/// </summary>
public class LLMInventoryResponse
{
    public int TotalModels { get; set; }
    public List<LLMModel> Models { get; set; } = new();
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Individual LLM model information
/// </summary>
public class LLMModel
{
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = "unknown";
    public string Type { get; set; } = string.Empty;
    public string[] Capabilities { get; set; } = Array.Empty<string>();
    public double? SizeGB { get; set; }
    public double? Cost { get; set; } // Cost per 1K tokens
}
