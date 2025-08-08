using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenWebUILiteLLM.AppHost;
using Projects;
using AspireExtensions.Extensions;
using AspireExtensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);
// Load and validate LiteLLM configuration
var liteLlmConfig = builder.Configuration.GetSection("LiteLLM").Get<AspireExtensions.Configuration.LiteLLMConfiguration>();
var postgresConfig = builder.Configuration.GetSection("Postgres").Get<AspireExtensions.Configuration.PostgresConfig>();
var openWebUiConfig = builder.Configuration.GetSection("OpenWebUI").Get<AspireExtensions.Configuration.OpenWebUiConfig>();
var networkConfig = builder.Configuration.GetSection("PublicNetwork").Get<AspireExtensions.Configuration.NetworkSettings>();

if (liteLlmConfig == null || postgresConfig == null || openWebUiConfig == null || networkConfig == null)
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
var litellm = builder.AddLiteLLM(
    liteLlmConfig,
    postgres,
    litellmDb,
    ollama,
    pgUsername,
    pgPassword,
    pgPort);
// Note: LiteLLM health check disabled due to authentication requirements
// Open-WebUI with Azure AD Authentication and health check
var openWebUi = builder.AddOpenWebUI(
    openWebUiConfig,
    liteLlmConfig,
    postgres,
    openWebUiDb,
    litellm,
    pgUsername,
    pgPassword,
    pgPort);
// n8n workflow automation container
var n8n = builder.AddN8n(
    networkConfig,
    postgres,
    n8nDb,
    pgUsername,
    pgPassword,
    pgPort);

// Development Container with SSH access for multi-language development
var devContainer = builder.AddDevContainer();

// Reverse Proxy
var reverseProxy = builder.AddProject<ReverseProxy>("reverseproxy")
    .WithExternalHttpEndpoints()
    .WaitFor(openWebUi)
    .WaitFor(litellm)
    .WaitFor(n8n);
    
// Add devContainer dependency if it was created successfully
if (devContainer != null)
{
    reverseProxy = reverseProxy.WaitFor(devContainer);
    Console.WriteLine("Reverse proxy will wait for development container to be ready");
}
else
{
    Console.WriteLine("Development container not available - reverse proxy will start without waiting");
}
// Build and run the application
var app = builder.Build();
// Optional: Add global health check monitoring
//app.Services.GetRequiredService<ILogger<program>>().LogInformation("Starting application with health checks enabled");</ program >
app.Run();