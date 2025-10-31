using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using WolfPackAI.AppBuilder.Configuration;
using WolfPackAI.AppBuilder.Resources;

namespace WolfPackAI.AppBuilder.Extensions
{
    public static class LiteLLMExtensions
    {
        public static IResourceBuilder<ContainerResource> AddLiteLLM(
            this IDistributedApplicationBuilder builder,
            LiteLLMConfiguration liteLlmConfig,
            IResourceBuilder<PostgresServerResource> postgres,
            IResourceBuilder<PostgresDatabaseResource> litellmDb,
            IResourceBuilder<OllamaResource> ollama,
            string postgresUsername = "postgres",
            string postgresPassword = "postgres",
            int postgresPort = 5432,
            string configMountPath = "./litellm-config.yaml",
            string uiUsername = "test",
            string uiPassword = "test",
            string logLevel = "DEBUG",
            string name = "litellm",
            string image = "litellm/litellm-database",
                string tag = "v1.79.1.dev1",
                string? openAiApiKey = null,
                string? anthropicApiKey = null)
        {
                var container = builder.AddContainer(name, image, tag)
                    .WithHttpEndpoint(port: 4000, targetPort: 4000, name: "http")
                    .WithEnvironment("STORE_MODEL_IN_DB", "True")
                    .WithEnvironment("LITELLM_MASTER_KEY", liteLlmConfig.GeneralSettings.MasterKey)
                    .WithEnvironment("LITELLM_LOG", logLevel)
                    .WithEnvironment("DATABASE_URL", $"postgresql://{postgresUsername}:{postgresPassword}@postgres:{postgresPort.ToString()}/litellmdb")
                    .WithEnvironment("UI_USERNAME", uiUsername)
                    .WithEnvironment("UI_PASSWORD", uiPassword)
                    .WithBindMount(configMountPath, "/app/config.yaml")
                    .WithArgs("--config", "/app/config.yaml")
                    .WithReference(postgres)
                    .WithReference(litellmDb)
                    .WithReference(ollama);

                if (!string.IsNullOrEmpty(openAiApiKey))
                {
                    container = container.WithEnvironment("OPENAI_API_KEY", openAiApiKey);
                }

                if (!string.IsNullOrEmpty(anthropicApiKey))
                {
                    container = container.WithEnvironment("ANTHROPIC_API_KEY", anthropicApiKey);
                }

                return container
                    .WaitFor(postgres)
                    .WaitFor(ollama);
        }

        public static IResourceBuilder<ContainerResource> AddLiteLLM(
            this IDistributedApplicationBuilder builder,
            string masterKey,
            IResourceBuilder<PostgresServerResource> postgres,
            IResourceBuilder<PostgresDatabaseResource> litellmDb,
            IResourceBuilder<OllamaResource> ollama,
            string postgresUsername = "postgres",
            string postgresPassword = "postgres",
            int postgresPort = 5432,
            string configMountPath = "./litellm-config.yaml",
            string uiUsername = "test",
            string uiPassword = "test",
            string serverRootPath = "/litellm",
            string logLevel = "DEBUG",
            string name = "litellm",
            string image = "litellm/litellm-database",
                string tag = "v1.79.1.dev1",
                string? openAiApiKey = null,
                string? anthropicApiKey = null)
        {
                var container = builder.AddContainer(name, image, tag)
                    .WithHttpEndpoint(port: 4000, targetPort: 4000, name: "http")
                    .WithEnvironment("STORE_MODEL_IN_DB", "True")
                    .WithEnvironment("LITELLM_MASTER_KEY", masterKey)
                    .WithEnvironment("LITELLM_LOG", logLevel)
                    .WithEnvironment("DATABASE_URL", $"postgresql://{postgresUsername}:{postgresPassword}@postgres:{postgresPort.ToString()}/litellmdb")
                    .WithEnvironment("UI_USERNAME", uiUsername)
                    .WithEnvironment("UI_PASSWORD", uiPassword)
                    .WithBindMount(configMountPath, "/app/config.yaml")
                    .WithArgs("--config", "/app/config.yaml")
                    .WithReference(postgres)
                    .WithReference(litellmDb)
                    .WithReference(ollama);

                if (!string.IsNullOrEmpty(openAiApiKey))
                {
                    container = container.WithEnvironment("OPENAI_API_KEY", openAiApiKey);
                }

                if (!string.IsNullOrEmpty(anthropicApiKey))
                {
                    container = container.WithEnvironment("ANTHROPIC_API_KEY", anthropicApiKey);
                }

                return container
                    .WaitFor(postgres)
                    .WaitFor(ollama);
        }
    }
}