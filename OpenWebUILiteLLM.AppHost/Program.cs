using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenWebUILiteLLM.AppHost;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);
// Load and validate LiteLLM configuration
var liteLlmConfig = builder.Configuration.GetSection("LiteLLM").Get<OpenWebUILiteLLM.AppHost.LiteLLMConfiguration>();
var authConfig = builder.Configuration.GetSection("Auth").Get<OpenWebUILiteLLM.AppHost.AuthConfig>();
var postgresConfig = builder.Configuration.GetSection("Postgres").Get<OpenWebUILiteLLM.AppHost.PostgresConfig>();
var openWebUiConfig = builder.Configuration.GetSection("OpenWebUI").Get<OpenWebUILiteLLM.AppHost.OpenWebUiConfig>();
var networkConfig = builder.Configuration.GetSection("PublicNetwork").Get<OpenWebUILiteLLM.AppHost.NetworkSettings>();

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
var usernameParam = builder.AddParameter("postgres-username", pgUsername);
var passwordParam = builder.AddParameter("postgres-password", pgPassword);
// PostgreSQL database for Open-WebUI with health check
var postgres = builder.AddPostgres("postgres",
    userName: usernameParam,
    password: passwordParam,
    port: pgPort)
    .WithDataVolume()
.WithPgAdmin();
var openWebUiDb = postgres.AddDatabase("openwebuidb");
var litellmDb = postgres.AddDatabase("litellmdb");
var n8nDb = postgres.AddDatabase("n8ndb");
// Ollama container
var ollama = builder.AddOllama("qwen3:0.6b", useGpu: true, hostPort: 1143);
// LiteLLM Proxy Configuration with health check
var litellm = builder.AddContainer("litellm", "ghcr.io/berriai/litellm-database", "main-v1.74.8-nightly")
.WithHttpEndpoint(port: 4000, targetPort: 4000, name: "http")
.WithEnvironment("STORE_MODEL_IN_DB", "True")
.WithEnvironment("LITELLM_MASTER_KEY", liteLlmConfig.GeneralSettings.MasterKey)
.WithEnvironment("LITELLM_LOG", "DEBUG")
.WithEnvironment("DATABASE_URL", $"postgresql://{pgUsername}:{pgPassword}@postgres:{pgPort.ToString()}/litellmdb")
.WithEnvironment("SERVER_ROOT_PATH", "/litellm")
.WithEnvironment("UI_USERNAME", "test")
.WithEnvironment("UI_PASSWORD", "test")
.WithBindMount("./litellm-config.yaml", "/app/config.yaml")
.WithArgs("--config", "/app/config.yaml")
.WithReference(postgres)
.WithReference(litellmDb)
.WithReference(ollama)
.WaitFor(postgres)
.WaitFor(ollama)
.WithHttpHealthCheck("/health", 401); // LiteLLM health check endpoint
// Open-WebUI with Azure AD Authentication and health check
var authRedirectUri = new Uri(new Uri(openWebUiConfig.PublicUrl), "/oauth/oidc/callback").ToString();
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
    .WithEnvironment("OPENID_REDIRECT_URI", authRedirectUri)
    .WithEnvironment("WEBUI_NAME", "AI Portal")
    .WithEnvironment("OPENAI_API_BASE_URL", litellm.GetEndpoint("http"))
    .WithEnvironment("OPENAI_API_KEY", liteLlmConfig.GeneralSettings.MasterKey)
    .WithEnvironment("DATABASE_URL", $"postgresql://{pgUsername}:{pgPassword}@postgres:{pgPort.ToString()}/openwebuidb")
    .WithEnvironment("CORS_ALLOW_ORIGIN", "*") // Set to specific origins in production, e.g., openWebUiConfig.PublicUrl
    .WithEnvironment("USER_AGENT", "OpenWebUI/v0.6.18")
    .WithReference(postgres)
    .WithReference(openWebUiDb)
    .WaitFor(postgres)
    .WaitFor(litellm)
    .WithHttpHealthCheck(path: "/health");
// n8n workflow automation container
// Add n8n workflow automation container with Postgres connection and persistent volume
var publicUrl = string.IsNullOrEmpty(networkConfig.PublicUrl) ? "localhost" : networkConfig.PublicUrl;
var protocol = string.IsNullOrEmpty(networkConfig.PublicUrl) ? "http" : "https";
var n8n = builder.AddContainer("n8n", "docker.n8n.io/n8nio/n8n")
.WithHttpEndpoint(port: 5678, targetPort: 5678, name: "http")
.WithVolume("n8n_data", "/home/node/.n8n")
.WithEnvironment("DB_TYPE", "postgresdb")
.WithEnvironment("N8N_PROTOCOL", protocol)
.WithEnvironment("N8N_HOST", publicUrl)
.WithEnvironment("N8N_PORT", "5678")
.WithEnvironment("N8N_PATH", "/n8n/")
.WithEnvironment("WEBHOOK_URL", $"{protocol}://{publicUrl}/n8n/")
.WithEnvironment("VUE_APP_URL_BASE_API", $"{protocol}://{publicUrl}/n8n/")
.WithEnvironment("N8N_EDITOR_BASE_URL", $"{protocol}://{publicUrl}/n8n/")
.WithEnvironment("DB_POSTGRESDB_DATABASE", "n8ndb")
.WithEnvironment("DB_POSTGRESDB_HOST", "postgres")
.WithEnvironment("DB_POSTGRESDB_PORT", pgPort.ToString())
.WithEnvironment("DB_POSTGRESDB_USER", pgUsername)
.WithEnvironment("DB_POSTGRESDB_PASSWORD", pgPassword)
.WithEnvironment("DB_POSTGRESDB_SCHEMA", "public")
.WithEnvironment("DB_POSTGRESDB_SSL", "false") // Set to "true" for production with SSL
.WithEnvironment("GENERIC_TIMEZONE", "Europe/London")
.WithEnvironment("TZ", "Europe/London")
.WithEnvironment("N8N_BASIC_AUTH_ACTIVE", "true")
.WithEnvironment("N8N_BASIC_AUTH_USER", "admin")
.WithEnvironment("N8N_BASIC_AUTH_PASSWORD", "your_secure_password")
.WithEnvironment("N8N_RUNNERS_ENABLED", "true")
.WithReference(postgres)
.WithReference(n8nDb)
.WaitFor(postgres)
.WithHttpHealthCheck("/healthz", 200);
// Reverse Proxy
var reverseProxy = builder.AddProject<ReverseProxy>("reverseproxy")
    .WithExternalHttpEndpoints()
    .WaitFor(openWebUi)
    .WaitFor(litellm)
    .WaitFor(n8n);
// Build and run the application
var app = builder.Build();
// Optional: Add global health check monitoring
//app.Services.GetRequiredService<ILogger<program>>().LogInformation("Starting application with health checks enabled");</ program >
app.Run();