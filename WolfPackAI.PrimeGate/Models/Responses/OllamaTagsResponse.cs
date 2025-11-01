namespace WolfPackAI.PrimeGate.Models.Responses;

/// <summary>
/// Response from Ollama /api/tags endpoint
/// </summary>
public class OllamaTagsResponse
{
    public OllamaModel[]? Models { get; set; }
}

/// <summary>
/// Individual Ollama model information
/// </summary>
public class OllamaModel
{
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Modified_At { get; set; } = string.Empty;
}
