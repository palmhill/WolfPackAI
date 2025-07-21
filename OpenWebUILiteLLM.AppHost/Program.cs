using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using System.IO;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Load and validate LiteLLM configuration
var config = builder.Configuration.GetSection("LiteLLM").Get<OpenWebUILiteLLM.AppHost.LiteLLMConfiguration>();
if (config == null)
{
    throw new InvalidOperationException("LiteLLM configuration is missing in appsettings.json");
}

try
{
    config.Validate();
    
    // Generate the litellm-config.yaml file from configuration
    var yamlContent = config.GenerateYaml();
    File.WriteAllText("litellm-config.yaml", yamlContent);
    Console.WriteLine("Successfully generated litellm-config.yaml from configuration");
}
catch (Exception ex)
{
    Console.WriteLine($"Configuration validation failed: {ex.Message}");
    throw;
}

// Configuration parameters from AppSettings
var azureAdTenantId = config.Auth.AzureAd.TenantId;
var azureAdClientId = config.Auth.AzureAd.ClientId;
var azureAdClientSecret = config.Auth.AzureAd.ClientSecret;

// PostgreSQL settings from configuration
var pgUsername = config.Postgres.Username;
var pgPassword = config.Postgres.Password;
var pgPort = config.Postgres.Port;

// Create parameters for Postgres username and password
// When using WithDataVolume(), credentials are persisted in the volume
// It's important to always use the same credentials or PostgreSQL will reject connections
var usernameParam = builder.AddParameter("postgres-username", pgUsername);
var passwordParam = builder.AddParameter("postgres-password", pgPassword);

// PostgreSQL database for Open-WebUI
var postgres = builder.AddPostgres("postgres", 
    userName: usernameParam,
    password: passwordParam,
    port: pgPort)
    .WithDataVolume() // This persists data across restarts - credentials must stay the same!
    .WithPgAdmin();

var openWebUiDb = postgres.AddDatabase("openwebuidb");

// Redis for session storage
var redis = builder.AddRedis("redis")
    .WithDataVolume();

// LiteLLM Proxy Configuration
var litellm = builder.AddContainer("litellm", "ghcr.io/berriai/litellm", "litellm_stable_release_branch-v1.74.3-stable.patch.3")
    .WithHttpEndpoint(port: 4000, targetPort: 4000, name: "http")
    .WithEnvironment("STORE_MODEL_IN_DB", "True")
    .WithEnvironment("LITELLM_MASTER_KEY", config.GeneralSettings.MasterKey)
    .WithEnvironment("LITELLM_LOG", "DEBUG")
    .WithEnvironment("DATABASE_URL", config.GeneralSettings.DatabaseUrl)
    .WithEnvironment("AZURE_AD_TENANT_ID", azureAdTenantId)
    .WithEnvironment("AZURE_AD_CLIENT_ID", azureAdClientId)
    .WithEnvironment("AZURE_AD_CLIENT_SECRET", azureAdClientSecret)
    .WithBindMount("./litellm-config.yaml", "/app/config.yaml")
    .WithArgs("--config", "/app/config.yaml")
    .WithReference(postgres);

// Open-WebUI with Azure AD Authentication
var openWebUi = builder.AddContainer("openwebui", "ghcr.io/open-webui/open-webui", "latest")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithEnvironment("ENABLE_OAUTH_SIGNUP", "true")
    .WithEnvironment("OAUTH_PROVIDER_NAME", "Azure AD")
    .WithEnvironment("OPENID_PROVIDER_URL", $"https://login.microsoftonline.com/{azureAdTenantId}/v2.0")
    .WithEnvironment("OAUTH_CLIENT_ID", azureAdClientId)
    .WithEnvironment("OAUTH_CLIENT_SECRET", azureAdClientSecret)
    .WithEnvironment("OAUTH_SCOPES", "openid profile email")
    .WithEnvironment("OAUTH_MERGE_ACCOUNTS_BY_EMAIL", "true")
    .WithEnvironment("WEBUI_AUTH", "true")
    .WithEnvironment("WEBUI_NAME", "Enterprise AI Portal")
    .WithEnvironment("OPENAI_API_BASE_URL", litellm.GetEndpoint("http"))
    .WithEnvironment("OPENAI_API_KEY", config.GeneralSettings.MasterKey)
    .WithEnvironment("DATABASE_URL", $"postgresql://{pgUsername}:{pgPassword}@postgres:{pgPort.ToString()}/openwebuidb")
    .WithReference(postgres)
    .WithReference(openWebUiDb)
    .WithReference(redis)
    .WaitFor(postgres)
    .WaitFor(litellm);

// Optional: Add a reverse proxy for better URL management
var reverseProxy = builder.AddProject<Projects.ReverseProxy>("reverseproxy");

builder.Build().Run();
