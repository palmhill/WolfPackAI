using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OpenWebUILiteLLM.AppHost;

public class LiteLLMConfiguration
{
    [Required]
    public List<ModelConfig> ModelList { get; set; } = [];
    
    [Required]
    public LiteLLMSettings Settings { get; set; } = new();
    
    [Required]
    public GeneralSettings GeneralSettings { get; set; } = new();
    
    [Required]
    public RouterSettings RouterSettings { get; set; } = new();
    
    [Required]
    public AuthConfig Auth { get; set; } = new();

    public void Validate()
    {
        // Validate each model config
        foreach (var model in ModelList)
        {
            if (string.IsNullOrEmpty(model.ModelName))
                throw new ValidationException("Model name cannot be empty");
            
            if (model.LiteLLMParams == null)
                throw new ValidationException($"LiteLLM params missing for model {model.ModelName}");
            
            if (string.IsNullOrEmpty(model.LiteLLMParams.Model))
                throw new ValidationException($"Model type missing for model {model.ModelName}");
            
            if (string.IsNullOrEmpty(model.LiteLLMParams.ApiBase))
                throw new ValidationException($"API base URL missing for model {model.ModelName}");
        }
        
        // Validate general settings
        if (string.IsNullOrEmpty(GeneralSettings.MasterKey))
            throw new ValidationException("Master key cannot be empty");
            
        if (string.IsNullOrEmpty(GeneralSettings.DatabaseUrl))
            throw new ValidationException("Database URL cannot be empty");
            
        // Validate Auth
        if (string.IsNullOrEmpty(Auth.AzureAd?.TenantId))
            throw new ValidationException("Azure AD Tenant ID cannot be empty");
            
        if (string.IsNullOrEmpty(Auth.AzureAd?.ClientId))
            throw new ValidationException("Azure AD Client ID cannot be empty");
    }

    public string GenerateYaml()
    {
        // Convert the C# configuration to the expected YAML format for LiteLLM
        var yamlObject = new Dictionary<string, object>
        {
            ["model_list"] = ModelList.Select(m => new Dictionary<string, object>
            {
                ["model_name"] = m.ModelName,
                ["litellm_params"] = new Dictionary<string, object>
                {
                    ["model"] = m.LiteLLMParams.Model,
                    ["api_base"] = m.LiteLLMParams.ApiBase,
                    ["api_key"] = $"os.environ/{m.LiteLLMParams.ApiKey}",
                    ["api_version"] = m.LiteLLMParams.ApiVersion
                }
            }).ToList(),
            
            ["litellm_settings"] = new Dictionary<string, object>
            {
                ["drop_params"] = Settings.DropParams,
                ["set_verbose"] = Settings.SetVerbose
            },
            
            ["general_settings"] = new Dictionary<string, object>
            {
                ["master_key"] = GeneralSettings.MasterKey,
                ["database_url"] = GeneralSettings.DatabaseUrl
            },
            
            ["router_settings"] = new Dictionary<string, object>
            {
                ["routing_strategy"] = RouterSettings.RoutingStrategy,
                ["num_retries"] = RouterSettings.NumRetries,
                ["timeout"] = RouterSettings.Timeout
            },
            
            ["environment_variables"] = new Dictionary<string, object>
            {
                ["AZURE_AD_TENANT_ID"] = Auth.AzureAd?.TenantId ?? "",
                ["AZURE_AD_CLIENT_ID"] = Auth.AzureAd?.ClientId ?? "",
                ["AZURE_AD_CLIENT_SECRET"] = Auth.AzureAd?.ClientSecret ?? ""
            }
        };
        
        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
            
        return serializer.Serialize(yamlObject);
    }
}

public class ModelConfig
{
    [JsonPropertyName("modelName")]
    [YamlMember(Alias = "model_name")]
    public string ModelName { get; set; } = string.Empty;
    
    [JsonPropertyName("liteLLMParams")]
    [YamlMember(Alias = "litellm_params")]
    public LiteLLMParams LiteLLMParams { get; set; } = new();
}

public class LiteLLMParams
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("apiBase")]
    [YamlMember(Alias = "api_base")]
    public string ApiBase { get; set; } = string.Empty;
    
    [JsonPropertyName("apiKey")]
    [YamlMember(Alias = "api_key")]
    public string ApiKey { get; set; } = string.Empty;
    
    [JsonPropertyName("apiVersion")]
    [YamlMember(Alias = "api_version")]
    public string ApiVersion { get; set; } = string.Empty;
}

public class LiteLLMSettings
{
    [JsonPropertyName("dropParams")]
    [YamlMember(Alias = "drop_params")]
    public bool DropParams { get; set; } = true;
    
    [JsonPropertyName("setVerbose")]
    [YamlMember(Alias = "set_verbose")]
    public bool SetVerbose { get; set; } = true;
}

public class GeneralSettings
{
    [JsonPropertyName("masterKey")]
    [YamlMember(Alias = "master_key")]
    public string MasterKey { get; set; } = string.Empty;
    
    [JsonPropertyName("databaseUrl")]
    [YamlMember(Alias = "database_url")]
    public string DatabaseUrl { get; set; } = string.Empty;
}

public class RouterSettings
{
    [JsonPropertyName("routingStrategy")]
    [YamlMember(Alias = "routing_strategy")]
    public string RoutingStrategy { get; set; } = "least-busy";
    
    [JsonPropertyName("numRetries")]
    [YamlMember(Alias = "num_retries")]
    public int NumRetries { get; set; } = 3;
    
    [JsonPropertyName("timeout")]
    public int Timeout { get; set; } = 600;
}

public class AuthConfig
{
    [JsonPropertyName("azureAd")]
    public AzureAdConfig AzureAd { get; set; } = new();
}

public class AzureAdConfig
{
    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = string.Empty;
    
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;
    
    [JsonPropertyName("clientSecret")]
    public string ClientSecret { get; set; } = string.Empty;
}