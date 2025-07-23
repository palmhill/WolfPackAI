using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using System.IO;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Load and validate LiteLLM configuration
var liteLlmConfig = builder.Configuration.GetSection("LiteLLM").Get<OpenWebUILiteLLM.AppHost.LiteLLMConfiguration>();
var authConfig = builder.Configuration.GetSection("Auth").Get<OpenWebUILiteLLM.AppHost.AuthConfig>();
var postgresConfig = builder.Configuration.GetSection("Postgres").Get<OpenWebUILiteLLM.AppHost.PostgresConfig>();
var openWebUiConfig = builder.Configuration.GetSection("OpenWebUI").Get<OpenWebUILiteLLM.AppHost.OpenWebUiConfig>();
var networkConfig = builder.Configuration.GetSection("PublicNetwork").Get<OpenWebUILiteLLM.AppHost.NetworkSettings >();


if (liteLlmConfig == null || authConfig == null || postgresConfig == null || openWebUiConfig == null || networkConfig == null)
{
    throw new InvalidOperationException("Configuration section is missing in appsettings.json");
}

try
{
    liteLlmConfig.Validate();
    
    // Generate the litellm-config.yaml file from configuration
    var yamlContent = liteLlmConfig.GenerateYaml();
    File.WriteAllText("litellm-config.yaml", yamlContent);
    Console.WriteLine("Successfully generated litellm-config.yaml from configuration");
}
catch (Exception ex)
{
    Console.WriteLine($"Configuration validation failed: {ex.Message}");
    throw;
}

// Configuration parameters from AppSettings
var azureAdTenantId = authConfig.AzureAd.TenantId;
var azureAdClientId = authConfig.AzureAd.ClientId;
var azureAdClientSecret = authConfig.AzureAd.ClientSecret;

// PostgreSQL settings from configuration
var pgUsername = postgresConfig.Username;
var pgPassword = postgresConfig.Password;
var pgPort = postgresConfig.Port;

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


// LiteLLM Proxy Configuration
var litellm = builder.AddContainer("litellm", "ghcr.io/berriai/litellm-database", "main-v1.74.3-stable.patch.4")
    .WithHttpEndpoint(port: 4000, targetPort: 4000, name: "http")
    .WithEnvironment("STORE_MODEL_IN_DB", "True")
    .WithEnvironment("LITELLM_MASTER_KEY", liteLlmConfig.GeneralSettings.MasterKey)
    .WithEnvironment("LITELLM_LOG", "DEBUG")
    .WithEnvironment("DATABASE_URL", $"postgresql://{pgUsername}:{pgPassword}@postgres:{pgPort.ToString()}/litellm")
    //.WithEnvironment("AZURE_AD_TENANT_ID", azureAdTenantId)
    //.WithEnvironment("AZURE_AD_CLIENT_ID", azureAdClientId)
    //.WithEnvironment("AZURE_AD_CLIENT_SECRET", azureAdClientSecret)
    .WithEnvironment("SERVER_ROOT_PATH", "/litellm")
    .WithEnvironment("PROXY_BASE_URL ", "/litellm")
    .WithEnvironment("UI_USERNAME", "test")
    .WithEnvironment("UI_PASSWORD", "test")
    .WithBindMount("./litellm-config.yaml", "/app/config.yaml")
    .WithArgs("--config", "/app/config.yaml")
    .WithReference(postgres);

// Open-WebUI with Azure AD Authentication
var openWebUi = builder.AddContainer("openwebui", "ghcr.io/open-webui/open-webui", "latest")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithEnvironment("ENABLE_PERSISTENT_CONFIG", "false")
    .WithEnvironment("WEBUI_URL", openWebUiConfig.PublicUrl)
    .WithEnvironment("ENABLE_OAUTH_SIGNUP", "true")
    .WithEnvironment("OAUTH_PROVIDER_NAME", "Azure AD")
    .WithEnvironment("OPENID_PROVIDER_URL", $"https://login.microsoftonline.com/{azureAdTenantId}/v2.0/.well-known/openid-configuration")
    .WithEnvironment("OAUTH_CLIENT_ID", azureAdClientId)
    .WithEnvironment("OAUTH_CLIENT_SECRET", azureAdClientSecret)
    .WithEnvironment("OAUTH_SCOPES", "openid profile email")
    .WithEnvironment("OAUTH_MERGE_ACCOUNTS_BY_EMAIL", "true")
    .WithEnvironment("WEBUI_AUTH", "true")
    .WithEnvironment("OPENID_REDIRECT_URI", "http://localhost/oauth/oidc/callback")
    .WithEnvironment("WEBUI_NAME", "AI Portal")
    .WithEnvironment("OPENAI_API_BASE_URL", litellm.GetEndpoint("http"))
    .WithEnvironment("OPENAI_API_KEY", liteLlmConfig.GeneralSettings.MasterKey)
    .WithEnvironment("DATABASE_URL", $"postgresql://{pgUsername}:{pgPassword}@postgres:{pgPort.ToString()}/openwebuidb")
    .WithReference(postgres)
    .WithReference(openWebUiDb)
    .WaitFor(postgres)
    .WaitFor(litellm);

// Optional: Add a reverse proxy for better URL management
var reverseProxy = builder.AddContainer("reverseproxy", "reverseproxy", "latest")
    .WithHttpEndpoint(port: networkConfig.HttpPort, targetPort: 8181, name: "http")
    .WithBindMount("../ReverseProxy/appsettings.json", "/app/appsettings.json")
    .WaitFor(openWebUi)
    .WaitFor(litellm);

builder.Build().Run();
