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
    public NetworkSettings PublicNetwork { get; set; }


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
                    ["api_version"] = m.LiteLLMParams.ApiVersion,
                    ["provider"] = m.LiteLLMParams.Provider
                }
            }).ToList(),
            
            ["litellm_settings"] = new Dictionary<string, object>
            {
                ["drop_params"] = Settings.DropParams,
                ["set_verbose"] = Settings.SetVerbose
            },
            
            //["general_settings"] = new Dictionary<string, object>
            //{
            //    ["master_key"] = GeneralSettings.MasterKey,
            //    ["database_url"] = GeneralSettings.DatabaseUrl
            //},
            
            ["router_settings"] = new Dictionary<string, object>
            {
                ["routing_strategy"] = RouterSettings.RoutingStrategy,
                ["num_retries"] = RouterSettings.NumRetries,
                ["timeout"] = RouterSettings.Timeout
            },
            
            //["environment_variables"] = new Dictionary<string, object>
            //{
            //    ["AZURE_AD_TENANT_ID"] = Auth.AzureAd?.TenantId ?? "",
            //    ["AZURE_AD_CLIENT_ID"] = Auth.AzureAd?.ClientId ?? "",
            //    ["AZURE_AD_CLIENT_SECRET"] = Auth.AzureAd?.ClientSecret ?? ""
            //}
        };
        
        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
            
        return serializer.Serialize(yamlObject);
    }
}

public class NetworkSettings
{
   public int HttpPort { get; set; } = 80;
   public int HttpsPort { get; set; } = 443;
}

public class OpenWebUiConfig
{
    public string PublicUrl { get; set; } = string.Empty;
}

public class ModelConfig
{
    [JsonPropertyName("modelName")]
    [YamlMember(Alias = "model_name")]
    public string ModelName { get; set; } = string.Empty;
    
    [JsonPropertyName("liteLLMParams")]
    [YamlMember(Alias = "litellm_params")]
    public LiteLLMParams LiteLLMParams { get; set; } = new();


    [YamlMember(Alias = "model_name")]
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "ollama";

    [JsonPropertyName("modelType")]
    [YamlMember(Alias = "model_type")]
    public string ModelType { get; set; } = string.Empty;

    [JsonPropertyName("apiBase")]
    [YamlMember(Alias = "api_base")]
    public string ApiBase { get; set; } = string.Empty;


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

    [JsonPropertyName("provider")]
    [YamlMember(Alias = "provider")]
    public string Provider { get; set; } = "ollama";
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

public class PostgresConfig
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "postgres";
    
    [JsonPropertyName("password")]
    public string Password { get; set; } = "postgres";
    
    [JsonPropertyName("port")]
    public int Port { get; set; } = 5432;
    
}