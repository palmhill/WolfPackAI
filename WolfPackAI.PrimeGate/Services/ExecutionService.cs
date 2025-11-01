using System.Text.Json;
using Microsoft.Extensions.Options;
using WolfPackAI.PrimeGate.Configuration;
using WolfPackAI.PrimeGate.Models.Requests;
using WolfPackAI.PrimeGate.Models.Responses;

namespace WolfPackAI.PrimeGate.Services;

/// <summary>
/// Service for executing tasks with selected LLM models
/// </summary>
public class ExecutionService : IExecutionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExecutionService> _logger;
    private readonly EndpointsConfiguration _endpoints;

    public ExecutionService(
        IHttpClientFactory httpClientFactory,
        ILogger<ExecutionService> logger,
        IOptions<EndpointsConfiguration> endpoints)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _endpoints = endpoints.Value;
    }

    /// <inheritdoc/>
    public async Task<ExecutionResponse> ExecuteAsync(ExecutionRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Executing request {RequestId} with model {Model}",
                request.RequestId,
                request.SelectedModel);

            // Determine the endpoint based on model type
            var endpoint = GetEndpointForModel(request.SelectedModel);

            // Create the HTTP client with appropriate timeout
            var client = _httpClientFactory.CreateClient();
            var timeout = DetermineTimeout(request.SelectedModel);
            client.Timeout = TimeSpan.FromSeconds(timeout);

            // Build the request payload
            var payload = BuildRequestPayload(request);

            // Execute the request
            var response = await client.PostAsJsonAsync(endpoint, payload);

            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Successfully executed request {RequestId} with model {Model}",
                    request.RequestId,
                    request.SelectedModel);

                return new ExecutionResponse
                {
                    Status = "success",
                    ModelUsed = request.SelectedModel,
                    Response = content,
                    OrchestratorRequestId = request.RequestId
                };
            }
            else
            {
                _logger.LogWarning(
                    "Request {RequestId} failed with status {StatusCode}",
                    request.RequestId,
                    response.StatusCode);

                return new ExecutionResponse
                {
                    Status = "error",
                    ModelUsed = request.SelectedModel,
                    Response = content,
                    OrchestratorRequestId = request.RequestId,
                    Message = $"HTTP {response.StatusCode}",
                    Note = "PrimeGate reports errors but lets orchestrator handle them"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error executing request {RequestId}", request.RequestId);

            return new ExecutionResponse
            {
                Status = "error",
                ModelUsed = request.SelectedModel,
                Response = string.Empty,
                OrchestratorRequestId = request.RequestId,
                Message = $"HTTP error: {ex.Message}",
                Note = "PrimeGate reports errors but lets orchestrator handle them"
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request {RequestId} timed out", request.RequestId);

            return new ExecutionResponse
            {
                Status = "error",
                ModelUsed = request.SelectedModel,
                Response = string.Empty,
                OrchestratorRequestId = request.RequestId,
                Message = "Request timed out",
                Note = "PrimeGate reports errors but lets orchestrator handle them"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing request {RequestId}", request.RequestId);

            return new ExecutionResponse
            {
                Status = "error",
                ModelUsed = request.SelectedModel,
                Response = string.Empty,
                OrchestratorRequestId = request.RequestId,
                Message = $"Unexpected error: {ex.Message}",
                Note = "PrimeGate reports errors but lets orchestrator handle them"
            };
        }
    }

    private string GetEndpointForModel(string modelName)
    {
        var modelLower = modelName.ToLowerInvariant();

        // Local models go to Ollama
        if (modelLower.Contains("deepseek") ||
            modelLower.Contains("qwen") ||
            modelLower.Contains("llama") ||
            modelLower.Contains("codellama") ||
            modelLower.Contains("mistral") && modelLower.Contains(":"))
        {
            var baseUrl = _endpoints.Ollama.BaseUrl.TrimEnd('/');
            var endpoint = _endpoints.Ollama.GenerateEndpoint.TrimStart('/');
            return $"{baseUrl}/{endpoint}";
        }

        // Cloud models and cursor models go to LiteLLM
        var litellmBaseUrl = _endpoints.LiteLLM.BaseUrl.TrimEnd('/');
        var litellmEndpoint = _endpoints.LiteLLM.ChatCompletionsEndpoint.TrimStart('/');
        return $"{litellmBaseUrl}/{litellmEndpoint}";
    }

    private object BuildRequestPayload(ExecutionRequest request)
    {
        var modelLower = request.SelectedModel.ToLowerInvariant();

        // Ollama-style payload for local models
        if (modelLower.Contains("deepseek") ||
            modelLower.Contains("qwen") ||
            modelLower.Contains("llama") ||
            modelLower.Contains("codellama") ||
            modelLower.Contains("mistral") && modelLower.Contains(":"))
        {
            var payload = new Dictionary<string, object>
            {
                ["model"] = request.SelectedModel,
                ["prompt"] = request.Prompt,
                ["stream"] = false
            };

            // Merge custom options if provided
            if (request.Options != null)
            {
                foreach (var option in request.Options)
                {
                    payload[option.Key] = option.Value;
                }
            }

            return payload;
        }

        // OpenAI-style payload for cloud models
        var messages = new[]
        {
            new { role = "user", content = request.Prompt }
        };

        var cloudPayload = new Dictionary<string, object>
        {
            ["model"] = request.SelectedModel,
            ["messages"] = messages
        };

        // Merge custom options if provided
        if (request.Options != null)
        {
            foreach (var option in request.Options)
            {
                cloudPayload[option.Key] = option.Value;
            }
        }

        return cloudPayload;
    }

    private int DetermineTimeout(string modelName)
    {
        var modelLower = modelName.ToLowerInvariant();

        // Local models use Ollama timeout
        if (modelLower.Contains("deepseek") ||
            modelLower.Contains("qwen") ||
            modelLower.Contains("llama") ||
            modelLower.Contains("codellama"))
        {
            return _endpoints.Ollama.Timeout;
        }

        // Cloud models use LiteLLM timeout
        return _endpoints.LiteLLM.Timeout;
    }
}
