using Microsoft.Extensions.Options;
using WolfPackAI.PrimeGate.Configuration;
using WolfPackAI.PrimeGate.Models.Requests;
using WolfPackAI.PrimeGate.Models.Responses;

namespace WolfPackAI.PrimeGate.Services;

/// <summary>
/// Service for suggesting optimal models based on task requirements
/// </summary>
public class ModelSuggestionService : IModelSuggestionService
{
    private readonly ILogger<ModelSuggestionService> _logger;
    private readonly ModelSuggestionConfiguration _config;

    public ModelSuggestionService(
        ILogger<ModelSuggestionService> logger,
        IOptions<ModelSuggestionConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    /// <inheritdoc/>
    public Task<SuggestionResponse> SuggestModelAsync(TaskRequest request, LLMInventoryResponse inventory)
    {
        if (!_config.EnableIntelligentSuggestions)
        {
            // Return first available model if intelligent suggestions are disabled
            var firstModel = inventory.Models.FirstOrDefault();
            var suggestion = new ModelSuggestion
            {
                ModelName = firstModel?.Name ?? "cursor-fast",
                Reason = "Intelligent suggestions disabled, using first available model",
                Confidence = _config.DefaultConfidence
            };

            return Task.FromResult(new SuggestionResponse
            {
                Suggestion = suggestion,
                AllAvailable = inventory.Models.Select(m => m.Name).ToList()
            });
        }

        var taskLower = request.Task.ToLowerInvariant();
        var contextLower = request.Context?.ToLowerInvariant() ?? string.Empty;
        var combinedText = $"{taskLower} {contextLower}";

        // Intelligent suggestion based on task analysis
        var modelSuggestion = AnalyzeTaskAndSuggest(combinedText, inventory);

        _logger.LogInformation(
            "Suggested model {ModelName} for task '{Task}' with confidence {Confidence}",
            modelSuggestion.ModelName,
            request.Task,
            modelSuggestion.Confidence);

        return Task.FromResult(new SuggestionResponse
        {
            Suggestion = modelSuggestion,
            AllAvailable = inventory.Models.Select(m => m.Name).ToList()
        });
    }

    private ModelSuggestion AnalyzeTaskAndSuggest(string combinedText, LLMInventoryResponse inventory)
    {
        // Priority order of task type detection
        var taskPatterns = new[]
        {
            (Keywords: new[] { "refactor", "restructure", "reorganize" }, PreferredKey: "Refactor"),
            (Keywords: new[] { "code review", "review code", "analyze code" }, PreferredKey: "CodeReview"),
            (Keywords: new[] { "algorithm", "complex logic", "optimization" }, PreferredKey: "Algorithm"),
            (Keywords: new[] { "security", "vulnerability", "exploit", "audit" }, PreferredKey: "Security"),
            (Keywords: new[] { "fast", "quick", "rapid", "immediate" }, PreferredKey: "Fast"),
            (Keywords: new[] { "documentation", "document", "comment", "explain" }, PreferredKey: "Documentation")
        };

        foreach (var (keywords, preferredKey) in taskPatterns)
        {
            if (keywords.Any(k => combinedText.Contains(k)))
            {
                if (_config.PreferredModels.TryGetValue(preferredKey, out var preferredModelPattern))
                {
                    var suggestion = FindModelByPattern(inventory, preferredModelPattern, GetReasonForKey(preferredKey));
                    if (suggestion != null)
                        return suggestion;
                }
            }
        }

        // Default fallback
        var firstModel = inventory.Models.FirstOrDefault();
        return new ModelSuggestion
        {
            ModelName = firstModel?.Name ?? "cursor-fast",
            Reason = "Default suggestion - no specific task pattern matched",
            Confidence = _config.DefaultConfidence
        };
    }

    private ModelSuggestion? FindModelByPattern(LLMInventoryResponse inventory, string pattern, string reason)
    {
        // Try exact match first
        var exactMatch = inventory.Models.FirstOrDefault(m =>
            m.Name.Equals(pattern, StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null)
        {
            return new ModelSuggestion
            {
                ModelName = exactMatch.Name,
                Reason = reason,
                Confidence = 0.9
            };
        }

        // Try partial match
        var partialMatch = inventory.Models.FirstOrDefault(m =>
            m.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase));

        if (partialMatch != null)
        {
            return new ModelSuggestion
            {
                ModelName = partialMatch.Name,
                Reason = $"{reason} (closest available match)",
                Confidence = 0.7
            };
        }

        // Pattern not found
        return null;
    }

    private static string GetReasonForKey(string key) => key switch
    {
        "Refactor" => "Best for code refactoring and restructuring",
        "CodeReview" => "Best for code review and analysis",
        "Algorithm" => "Best for complex algorithms and logic",
        "Security" => "Most thorough for security audits",
        "Fast" => "Fastest local model available",
        "Documentation" => "Cost-effective for documentation tasks",
        _ => "Suggested model for this task type"
    };
}
