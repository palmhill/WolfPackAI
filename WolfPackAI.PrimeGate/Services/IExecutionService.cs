using WolfPackAI.PrimeGate.Models.Requests;
using WolfPackAI.PrimeGate.Models.Responses;

namespace WolfPackAI.PrimeGate.Services;

/// <summary>
/// Service for executing tasks with selected LLM models
/// </summary>
public interface IExecutionService
{
    /// <summary>
    /// Execute a task with the specified model
    /// </summary>
    /// <param name="request">Execution request with model selection and prompt</param>
    /// <returns>Execution response with results or error information</returns>
    Task<ExecutionResponse> ExecuteAsync(ExecutionRequest request);
}
