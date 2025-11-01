namespace WolfPackAI.PrimeGate.Models.Requests;

/// <summary>
/// Request for /api/execute endpoint
/// </summary>
public class ExecutionRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string SelectedModel { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public Dictionary<string, object>? Options { get; set; }
}
