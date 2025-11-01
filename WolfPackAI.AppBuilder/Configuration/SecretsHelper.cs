using Microsoft.Extensions.Configuration;

namespace WolfPackAI.AppBuilder.Configuration;

/// <summary>
/// Helper class for managing environment variable substitution in configuration
/// </summary>
public static class SecretsHelper
{
    /// <summary>
    /// Resolves environment variable placeholders in configuration strings
    /// Example: "${MY_VAR}" becomes the value of environment variable MY_VAR
    /// </summary>
    public static string ResolveEnvironmentVariable(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        // Pattern: ${VAR_NAME}
        if (value.StartsWith("${") && value.EndsWith("}"))
        {
            var envVarName = value.Substring(2, value.Length - 3);
            var envValue = Environment.GetEnvironmentVariable(envVarName);

            if (string.IsNullOrEmpty(envValue))
            {
                throw new InvalidOperationException(
                    $"Environment variable '{envVarName}' is required but not set. " +
                    $"Please set it in your environment or .env file.");
            }

            return envValue;
        }

        return value;
    }

    /// <summary>
    /// Loads .env file if it exists
    /// </summary>
    public static void LoadDotEnvFile(string filePath = ".env")
    {
        if (!File.Exists(filePath))
            return;

        foreach (var line in File.ReadAllLines(filePath))
        {
            var trimmedLine = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;

            var parts = trimmedLine.Split('=', 2);
            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            // Don't override existing environment variables
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    /// <summary>
    /// Validates that all required environment variables are set
    /// </summary>
    public static void ValidateRequiredEnvironmentVariables(params string[] requiredVars)
    {
        var missing = requiredVars
            .Where(var => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(var)))
            .ToList();

        if (missing.Any())
        {
            throw new InvalidOperationException(
                $"Required environment variables are missing: {string.Join(", ", missing)}. " +
                $"Please set them in your environment or .env file.");
        }
    }

    /// <summary>
    /// Gets an environment variable with a fallback default value
    /// </summary>
    public static string GetEnvironmentVariableOrDefault(string variableName, string defaultValue)
    {
        return Environment.GetEnvironmentVariable(variableName) ?? defaultValue;
    }
}
