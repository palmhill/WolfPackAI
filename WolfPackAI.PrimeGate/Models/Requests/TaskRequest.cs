namespace WolfPackAI.PrimeGate.Models.Requests;

/// <summary>
/// Request for /api/suggest endpoint
/// </summary>
public class TaskRequest
{
    public string Task { get; set; } = string.Empty;
    public string? Context { get; set; }
}
