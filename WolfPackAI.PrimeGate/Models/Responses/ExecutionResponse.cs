namespace WolfPackAI.PrimeGate.Models.Responses;

/// <summary>
/// Response for /api/execute endpoint
/// </summary>
public class ExecutionResponse
{
    public string Status { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public string? OrchestratorRequestId { get; set; }
    public string? Message { get; set; }
    public string? Note { get; set; }
}
