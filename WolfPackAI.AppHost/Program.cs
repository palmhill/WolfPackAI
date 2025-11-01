using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Projects;
using WolfPackAI.AppBuilder.Extensions;

var builder = DistributedApplication.CreateBuilder(args);
// Load and validate LiteLLM configuration
var liteLlmConfig = builder.Configuration.GetSection("LiteLLM").Get<WolfPackAI.AppBuilder.Configuration.LiteLLMConfiguration>();
var postgresConfig = builder.Configuration.GetSection("Postgres").Get<WolfPackAI.AppBuilder.Configuration.PostgresConfig>();
var openWebUiConfig = builder.Configuration.GetSection("OpenWebUI").Get<WolfPackAI.AppBuilder.Configuration.OpenWebUiConfig>();
var n8nConfig = builder.Configuration.GetSection("n8n").Get<WolfPackAI.AppBuilder.Configuration.n8nConfig>();
var networkConfig = builder.Configuration.GetSection("Dashboard").Get<WolfPackAI.AppBuilder.Configuration.DashboardSettings>();
var sslConfig = builder.Configuration.GetSection("LiteLLMSSL").Get<WolfPackAI.AppBuilder.Configuration.LiteLLMSSLConfig>() 
    ?? new WolfPackAI.AppBuilder.Configuration.LiteLLMSSLConfig();

if (liteLlmConfig == null || postgresConfig == null || openWebUiConfig == null || networkConfig == null || n8nConfig == null)
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
var ollama = builder.AddOllama(liteLlmConfig.ModelList.First().ModelName, useGpu: true, hostPort: 1143);
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

// Add nginx reverse proxy with SSL if enabled
IResourceBuilder<ContainerResource>? nginx = null;
IResourceBuilder<ContainerResource>? certbot = null;
if (sslConfig.Enabled)
{
    try
    {
        sslConfig.Validate();
        // Generate nginx config with domain substitution
        var nginxConfigContent = File.ReadAllText("nginx-litellm.conf");
        nginxConfigContent = nginxConfigContent.Replace("${DOMAIN}", sslConfig.Domain);
        var nginxConfigPath = "nginx-litellm-generated.conf";
        File.WriteAllText(nginxConfigPath, nginxConfigContent);
        Console.WriteLine($"Generated nginx configuration for domain: {sslConfig.Domain}");
        
        // Note: Temporary self-signed certificates will be needed for initial nginx startup
        // Certbot will replace them with Let's Encrypt certificates
        // Users can generate them manually or use the init script approach
        Console.WriteLine($"Note: Ensure temporary SSL certificates exist at /etc/letsencrypt/live/{sslConfig.Domain}/");
        Console.WriteLine($"      Or nginx will generate them automatically on first start (if using custom entrypoint)");
        
        (nginx, certbot) = builder.AddNginxWithSSL(sslConfig, nginxConfigPath);
        Console.WriteLine($"SSL enabled for LiteLLM on domain: {sslConfig.Domain}");
        Console.WriteLine($"Access LiteLLM at: https://{sslConfig.Domain}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"SSL configuration failed: {ex.Message}");
        throw;
    }
}
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
    n8nConfig.Port,
    postgres,
    n8nDb,
    pgUsername,
    pgPassword,
    pgPort);

// PrimeGate service for Cursor integration
var primegate = builder.AddPrimeGate(litellm, ollama);

// Dashboard Proxy
var dashboard = builder.AddProject<Projects.WolfPackAI_Dashboard>("dashboard")
    .WithExternalHttpEndpoints()
    .WaitFor(openWebUi)
    .WaitFor(litellm)
    .WaitFor(n8n)
    .WaitFor(primegate);


// Build and run the application
var app = builder.Build();
// Optional: Add global health check monitoring
//app.Services.GetRequiredService<ILogger<program>>().LogInformation("Starting application with health checks enabled");
app.Run();