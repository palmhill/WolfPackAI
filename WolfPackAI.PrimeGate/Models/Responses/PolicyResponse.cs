namespace WolfPackAI.PrimeGate.Models.Responses;

/// <summary>
/// Response for /api/policy endpoint
/// </summary>
public class PolicyResponse
{
    public int TokenLimit { get; set; }
    public int RequestsPerMinute { get; set; }
    public int ConcurrentModels { get; set; }
    public double CostLimit { get; set; }
    public List<string> Restrictions { get; set; } = new();
    public string Note { get; set; } = "Orchestrator has unlimited access to all resources";
}
