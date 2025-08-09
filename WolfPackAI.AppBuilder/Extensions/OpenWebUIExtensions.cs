using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using WolfPackAI.AppBuilder.Configuration;

namespace WolfPackAI.AppBuilder.Extensions
{
    public static class OpenWebUIExtensions
    {
        public static IResourceBuilder<ContainerResource> AddOpenWebUI(
            this IDistributedApplicationBuilder builder,
            OpenWebUiConfig openWebUiConfig,
            LiteLLMConfiguration liteLlmConfig,
            IResourceBuilder<PostgresServerResource> postgres,
            IResourceBuilder<PostgresDatabaseResource> openWebUiDb,
            IResourceBuilder<ContainerResource> litellm,
            string postgresUsername = "postgres",
            string postgresPassword = "postgres",
            int postgresPort = 5432,
            string name = "openwebui",
            string tag = "latest")
        {
            var authRedirectUri = new Uri(new Uri(openWebUiConfig.PublicUrl), "/oauth/oidc/callback").ToString();
            
            return builder.AddContainer(name, "ghcr.io/open-webui/open-webui", tag)
                .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
                .WithEnvironment("ENABLE_PERSISTENT_CONFIG", "false")
                .WithEnvironment("WEBUI_URL", openWebUiConfig.PublicUrl)
                .WithEnvironment("WEBUI_AUTH", "true")
                .WithEnvironment("WEBUI_NAME", "AI Portal")
                .WithEnvironment("OPENAI_API_BASE_URL", litellm.GetEndpoint("http"))
                .WithEnvironment("OPENAI_API_KEY", liteLlmConfig.GeneralSettings.MasterKey)
                .WithEnvironment("DATABASE_URL", $"postgresql://{postgresUsername}:{postgresPassword}@postgres:{postgresPort.ToString()}/openwebuidb")
                .WithEnvironment("CORS_ALLOW_ORIGIN", "*") // Set to specific origins in production, e.g., openWebUiConfig.PublicUrl
                .WithEnvironment("USER_AGENT", "OpenWebUI/v0.6.18")
                .WithReference(postgres)
                .WithReference(openWebUiDb)
                .WaitFor(postgres)
                .WaitFor(litellm)
                .WithHttpHealthCheck(path: "/health");
        }

        public static IResourceBuilder<ContainerResource> AddOpenWebUI(
            this IDistributedApplicationBuilder builder,
            string publicUrl,
            string litellmMasterKey,
            IResourceBuilder<PostgresServerResource> postgres,
            IResourceBuilder<PostgresDatabaseResource> openWebUiDb,
            IResourceBuilder<ContainerResource> litellm,
            string postgresUsername = "postgres",
            string postgresPassword = "postgres", 
            int postgresPort = 5432,
            string name = "openwebui",
            string tag = "latest")
        {
            return builder.AddContainer(name, "ghcr.io/open-webui/open-webui", tag)
                .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
                .WithEnvironment("ENABLE_PERSISTENT_CONFIG", "false")
                .WithEnvironment("WEBUI_URL", publicUrl)
                .WithEnvironment("WEBUI_AUTH", "true")
                .WithEnvironment("WEBUI_NAME", "AI Portal")
                .WithEnvironment("OPENAI_API_BASE_URL", litellm.GetEndpoint("http"))
                .WithEnvironment("OPENAI_API_KEY", litellmMasterKey)
                .WithEnvironment("DATABASE_URL", $"postgresql://{postgresUsername}:{postgresPassword}@postgres:{postgresPort.ToString()}/openwebuidb")
                .WithEnvironment("CORS_ALLOW_ORIGIN", "*")
                .WithEnvironment("USER_AGENT", "OpenWebUI/v0.6.18")
                .WithReference(postgres)
                .WithReference(openWebUiDb)
                .WaitFor(postgres)
                .WaitFor(litellm)
                .WithHttpHealthCheck(path: "/health");
        }
    }
}