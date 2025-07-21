using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// Configuration parameters
var azureAdTenantId = builder.AddParameter("AzureAdTenantId", secret: true);
var azureAdClientId = builder.AddParameter("AzureAdClientId", secret: true);
var azureAdClientSecret = builder.AddParameter("AzureAdClientSecret", secret: true);

// PostgreSQL database for Open-WebUI
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var openWebUiDb = postgres.AddDatabase("openwebui");

// Redis for session storage
var redis = builder.AddRedis("redis")
    .WithDataVolume();

// LiteLLM Proxy Configuration
var litellm = builder.AddContainer("litellm", "ghcr.io/berriai/litellm", "latest")
    .WithHttpEndpoint(port: 4000, targetPort: 4000, name: "http")
    .WithEnvironment("LITELLM_MASTER_KEY", "sk-1234")
    .WithEnvironment("LITELLM_LOG", "DEBUG")
    .WithEnvironment("DATABASE_URL", "postgresql://postgres:postgres@postgres:5432/litellm")
    .WithBindMount("./litellm-config.yaml", "/app/config.yaml")
    .WithArgs("--config", "/app/config.yaml")
    .WithReference(postgres);

// Open-WebUI with Azure AD Authentication
var openWebUi = builder.AddContainer("openwebui", "ghcr.io/open-webui/open-webui", "latest")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithEnvironment("ENABLE_OAUTH_SIGNUP", "true")
    .WithEnvironment("OAUTH_PROVIDER_NAME", "Azure AD")
    .WithEnvironment("OPENID_PROVIDER_URL", () => $"https://login.microsoftonline.com/{azureAdTenantId.Resource.Value}/v2.0")
    .WithEnvironment("OAUTH_CLIENT_ID", azureAdClientId)
    .WithEnvironment("OAUTH_CLIENT_SECRET", azureAdClientSecret)
    .WithEnvironment("OAUTH_SCOPES", "openid profile email")
    .WithEnvironment("OAUTH_MERGE_ACCOUNTS_BY_EMAIL", "true")
    .WithEnvironment("WEBUI_AUTH", "true")
    .WithEnvironment("WEBUI_NAME", "Enterprise AI Portal")
    .WithEnvironment("OPENAI_API_BASE_URL", litellm.GetEndpoint("http"))
    .WithEnvironment("OPENAI_API_KEY", "sk-1234")
    .WithEnvironment("DATABASE_URL", () => $"{openWebUiDb.Resource.ConnectionStringExpression}")
    .WithReference(openWebUiDb)
    .WithReference(redis)
    .WaitFor(postgres)
    .WaitFor(litellm);

// Optional: Add a reverse proxy for better URL management
var reverseProxy = builder.AddProject<Projects.ReverseProxy>("reverseproxy");

builder.Build().Run();
