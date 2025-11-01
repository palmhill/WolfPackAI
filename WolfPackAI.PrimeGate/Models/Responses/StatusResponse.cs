namespace WolfPackAI.PrimeGate.Models.Responses;

/// <summary>
/// Response for /api/status endpoint
/// </summary>
public class StatusResponse
{
    public string Service { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string OrchestratorAuthority { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
