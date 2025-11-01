namespace WolfPackAI.PrimeGate.Models.Responses;

/// <summary>
/// Response for /api/suggest endpoint
/// </summary>
public class SuggestionResponse
{
    public ModelSuggestion Suggestion { get; set; } = new();
    public string Note { get; set; } = "This is a suggestion only. Orchestrator has final decision.";
    public List<string> AllAvailable { get; set; } = new();
}

/// <summary>
/// Model suggestion with reasoning
/// </summary>
public class ModelSuggestion
{
    public string ModelName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public double Confidence { get; set; }
}
