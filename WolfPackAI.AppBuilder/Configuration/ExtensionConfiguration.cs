using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WolfPackAI.AppBuilder.Configuration;

public class NetworkSettings
{
    [JsonPropertyName("httpPort")]
    public int HttpPort { get; set; } = 80;
    [JsonPropertyName("httpsPort")]
    public int HttpsPort { get; set; } = 443;
    [JsonPropertyName("publicUrl")]
    public string PublicUrl { get; set; } = string.Empty;
}

public class OpenWebUiConfig
{
    public string PublicUrl { get; set; } = string.Empty;
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
    public NetworkSettings PublicNetwork { get; set; } = new();

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
                    ["api_base"] = m.LiteLLMParams.ApiBase
                }
            }).ToList(),
            ["litellm_settings"] = new Dictionary<string, object>
            {
                ["drop_params"] = Settings.DropParams,
                ["set_verbose"] = Settings.SetVerbose
            },
            ["router_settings"] = new Dictionary<string, object>
            {
                ["routing_strategy"] = RouterSettings.RoutingStrategy,
                ["num_retries"] = RouterSettings.NumRetries,
                ["timeout"] = RouterSettings.Timeout
            },
        };
        
        // Conditionally add optional parameters to litellm_params
        foreach (var modelDict in (List<Dictionary<string, object>>)yamlObject["model_list"])
        {
            var paramsDict = (Dictionary<string, object>)modelDict["litellm_params"];
            var modelConfig = ModelList.First(m => m.ModelName == (string)modelDict["model_name"]);
            if (!string.IsNullOrEmpty(modelConfig.LiteLLMParams.ApiKey))
            {
                paramsDict["api_key"] = $"os.environ/{modelConfig.LiteLLMParams.ApiKey}";
            }
            if (!string.IsNullOrEmpty(modelConfig.LiteLLMParams.ApiVersion))
            {
                paramsDict["api_version"] = modelConfig.LiteLLMParams.ApiVersion;
            }
        }
        
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
    [JsonPropertyName("modelType")]
    [YamlMember(Alias = "model_type")]
    public string ModelType { get; set; } = string.Empty;
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