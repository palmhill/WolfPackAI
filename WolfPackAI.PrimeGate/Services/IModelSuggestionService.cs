using WolfPackAI.PrimeGate.Models.Requests;
using WolfPackAI.PrimeGate.Models.Responses;

namespace WolfPackAI.PrimeGate.Services;

/// <summary>
/// Service for suggesting optimal models for tasks
/// </summary>
public interface IModelSuggestionService
{
    /// <summary>
    /// Suggest the best model for a given task
    /// </summary>
    /// <param name="request">Task request with task description and context</param>
    /// <param name="inventory">Current LLM inventory</param>
    /// <returns>Model suggestion with reasoning</returns>
    Task<SuggestionResponse> SuggestModelAsync(TaskRequest request, LLMInventoryResponse inventory);
}
