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
.WithEnvironment("PROXY_HOST", "localhost")
.WithEnvironment("UI_USERNAME", "test")
.WithEnvironment("UI_PASSWORD", "test")
.WithBindMount("./litellm-config.yaml", "/app/config.yaml")
.WithArgs("--config", "/app/config.yaml")
.WithReference(postgres)
.WithReference(litellmDb)
.WithReference(ollama)
.WaitFor(postgres)
.WaitFor(ollama); 
// Note: LiteLLM health check disabled due to authentication requirements
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

// Development Container with SSH access for multi-language development
// First attempt with regular container - if this fails, we can fall back to Dockerfile approach
var devContainer = (IResourceBuilder<ContainerResource>?)null;
try
{
    devContainer = builder.AddContainer("devcontainer", "ubuntu", "24.04")
        .WithEndpoint(targetPort: 22, port: 2222, scheme: "tcp", name: "ssh")
        .WithHttpEndpoint(targetPort: 3456, port: 3456, name: "ccr-http")
        .WithBindMount("./code", "/app/code")
        .WithEntrypoint("/bin/bash")
        .WithArgs(
            "-c",
            "export DEBIAN_FRONTEND=noninteractive && " +
            // Basic system setup
            "apt-get update && " +
            "apt-get install -y openssh-server curl software-properties-common ca-certificates gnupg git vim nano make gcc g++ cmake libtool autoconf automake libc6-dev libstdc++6 python3-pip python3-venv sudo build-essential netcat-openbsd && " +
            
            // Add .NET repository
            "add-apt-repository ppa:dotnet/backports -y && " +
            
            // Add Node.js repository  
            "mkdir -p /etc/apt/keyrings && " +
            "curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg && " +
            "echo 'deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_22.x nodistro main' > /etc/apt/sources.list.d/nodesource.list && " +
            
            // Install .NET and Node.js
            "apt-get update && " +
            "apt-get install -y dotnet-sdk-9.0 nodejs && " +
            
            // Setup SSH
            "mkdir -p /var/run/sshd && " +
            "useradd -m -s /bin/bash developer && " +
            "echo 'developer:devpassword' | chpasswd && " +
            "echo 'root:supersecurepassword' | chpasswd && " +
            "usermod -aG sudo developer && " +
            "sed -i 's/#PermitRootLogin yes/PermitRootLogin yes/' /etc/ssh/sshd_config && " +
            "sed -i 's/#PasswordAuthentication no/PasswordAuthentication yes/' /etc/ssh/sshd_config && " +
            
            // Install CCR packages
            "npm install -g @anthropic-ai/claude-code @musistudio/claude-code-router && " +
            
            // Setup CCR directories and config
            "mkdir -p /root/.claude-code-router /home/developer/.claude-code-router && " +
            "chown -R developer:developer /home/developer/.claude-code-router && " +
            
            // Create CCR config
            "cat > /root/.claude-code-router/config.json << 'EOF'\n" +
            "{\n" +
            "  \"APIKEY\": \"ccr-dev-key-2024\",\n" +
            "  \"API_TIMEOUT_MS\": 600000,\n" +
            "  \"HOST\": \"0.0.0.0\",\n" +
            "  \"PORT\": 3456,\n" +
            "  \"ENABLE_ROUTER\": true,\n" +
            "  \"providers\": [\n" +
            "    {\n" +
            "      \"name\": \"litellm-fallback\",\n" +
            "      \"api_base_url\": \"http://litellm:4000/v1\",\n" +
            "      \"api_key\": \"your-litellm-key\",\n" +
            "      \"models\": [\"claude-3-5-sonnet\"],\n" +
            "      \"transformers\": [\"Anthropic\"]\n" +
            "    }\n" +
            "  ],\n" +
            "  \"router\": {\n" +
            "    \"default\": \"litellm-fallback,claude-3-5-sonnet\"\n" +
            "  },\n" +
            "  \"security\": {\n" +
            "    \"allowedOrigins\": [\"http://localhost:5000\", \"http://localhost:3456\", \"https://localhost:5000\"],\n" +
            "    \"enableCORS\": true,\n" +
            "    \"rateLimitRequests\": 100,\n" +
            "    \"rateLimitWindow\": 60000\n" +
            "  },\n" +
            "  \"ui\": {\n" +
            "    \"enabled\": true,\n" +
            "    \"theme\": \"dark\",\n" +
            "    \"showAdvanced\": true,\n" +
            "    \"autoRefresh\": true,\n" +
            "    \"basePath\": \"/ccr\"\n" +
            "  }\n" +
            "}\n" +
            "EOF\n" +
            
            // Copy config for developer user
            "cp /root/.claude-code-router/config.json /home/developer/.claude-code-router/config.json && " +
            "chown developer:developer /home/developer/.claude-code-router/config.json && " +
            
            // Start services in background and keep container running
            "/usr/sbin/sshd && " +
            "su - developer -c 'export HOME=/home/developer && cd ~ && nohup ccr start > ccr.log 2>&1 &' && " +
            
            // Wait for CCR to be ready
            "echo 'Waiting for CCR to start...' && " +
            "for i in {1..30}; do if nc -z localhost 3456 2>/dev/null; then echo 'CCR is ready on port 3456!'; break; fi; sleep 2; done && " +
            "echo 'Setup complete!' && " +
            
            // Keep container running
            "exec tail -f /dev/null"
        )

        .WithEnvironment("DEBIAN_FRONTEND", "noninteractive")
        .WithEnvironment("TZ", "UTC");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to configure development container: {ex.Message}");
    // Container setup will be skipped if there's an error
}

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