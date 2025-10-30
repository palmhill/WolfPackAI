using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using WolfPackAI.AppBuilder.Configuration;

namespace WolfPackAI.AppBuilder.Extensions
{
    public static class N8nExtensions
    {
        public static IResourceBuilder<ContainerResource> AddN8n(
            this IDistributedApplicationBuilder builder,
            int port,
            IResourceBuilder<PostgresServerResource> postgres,
            IResourceBuilder<PostgresDatabaseResource> n8nDb,
            string postgresUsername = "postgres",
            string postgresPassword = "postgres",
            int postgresPort = 5432,
            string basicAuthUser = "admin",
            string basicAuthPassword = "your_secure_password",
            string timezone = "Europe/London",
            string name = "n8n",
            string tag = "latest")
        {
    
            return builder.AddContainer(name, "docker.n8n.io/n8nio/n8n", tag)
                .WithHttpEndpoint(port: 5678, targetPort: 5678, name: "http")
                .WithVolume("n8n_data", "/home/node/.n8n")
                .WithEnvironment("DB_TYPE", "postgresdb")
                .WithEnvironment("N8N_PORT", port.ToString())
                .WithEnvironment("DB_POSTGRESDB_DATABASE", "n8ndb")
                .WithEnvironment("DB_POSTGRESDB_HOST", "postgres")
                .WithEnvironment("DB_POSTGRESDB_PORT", postgresPort.ToString())
                .WithEnvironment("DB_POSTGRESDB_USER", postgresUsername)
                .WithEnvironment("DB_POSTGRESDB_PASSWORD", postgresPassword)
                .WithEnvironment("DB_POSTGRESDB_SCHEMA", "public")
                .WithEnvironment("DB_POSTGRESDB_SSL", "false") // Set to "true" for production with SSL
                .WithEnvironment("GENERIC_TIMEZONE", timezone)
                .WithEnvironment("TZ", timezone)
                .WithEnvironment("N8N_BASIC_AUTH_ACTIVE", "true")
                .WithEnvironment("N8N_BASIC_AUTH_USER", basicAuthUser)
                .WithEnvironment("N8N_BASIC_AUTH_PASSWORD", basicAuthPassword)
                .WithEnvironment("N8N_RUNNERS_ENABLED", "true")
                .WithReference(postgres)
                .WithReference(n8nDb)
                .WaitFor(postgres)
                .WithHttpHealthCheck("/healthz", 200);
        }

        public static IResourceBuilder<ContainerResource> AddN8n(
            this IDistributedApplicationBuilder builder,
            string publicUrl,
            IResourceBuilder<PostgresServerResource> postgres,
            IResourceBuilder<PostgresDatabaseResource> n8nDb,
            string postgresUsername = "postgres",
            string postgresPassword = "postgres",
            int postgresPort = 5432,
            string basicAuthUser = "admin",
            string basicAuthPassword = "your_secure_password",
            string timezone = "Europe/London",
            string name = "n8n",
            string tag = "latest")
        {
            var protocol = string.IsNullOrEmpty(publicUrl) || publicUrl == "localhost" ? "http" : "https";
            var hostUrl = string.IsNullOrEmpty(publicUrl) ? "localhost" : publicUrl;

            return builder.AddContainer(name, "docker.n8n.io/n8nio/n8n", tag)
                .WithHttpEndpoint(port: 5678, targetPort: 5678, name: "http")
                .WithVolume("n8n_data", "/home/node/.n8n")
                .WithEnvironment("DB_TYPE", "postgresdb")
                .WithEnvironment("N8N_PROTOCOL", protocol)
                .WithEnvironment("N8N_HOST", hostUrl)
                .WithEnvironment("N8N_PORT", "5678")
                .WithEnvironment("N8N_PATH", "/n8n/")
                .WithEnvironment("WEBHOOK_URL", $"{protocol}://{hostUrl}/n8n/")
                .WithEnvironment("VUE_APP_URL_BASE_API", $"{protocol}://{hostUrl}/n8n/")
                .WithEnvironment("N8N_EDITOR_BASE_URL", $"{protocol}://{hostUrl}/n8n/")
                .WithEnvironment("DB_POSTGRESDB_DATABASE", "n8ndb")
                .WithEnvironment("DB_POSTGRESDB_HOST", "postgres")
                .WithEnvironment("DB_POSTGRESDB_PORT", postgresPort.ToString())
                .WithEnvironment("DB_POSTGRESDB_USER", postgresUsername)
                .WithEnvironment("DB_POSTGRESDB_PASSWORD", postgresPassword)
                .WithEnvironment("DB_POSTGRESDB_SCHEMA", "public")
                .WithEnvironment("DB_POSTGRESDB_SSL", "false")
                .WithEnvironment("GENERIC_TIMEZONE", timezone)
                .WithEnvironment("TZ", timezone)
                .WithEnvironment("N8N_BASIC_AUTH_ACTIVE", "true")
                .WithEnvironment("N8N_BASIC_AUTH_USER", basicAuthUser)
                .WithEnvironment("N8N_BASIC_AUTH_PASSWORD", basicAuthPassword)
                .WithEnvironment("N8N_RUNNERS_ENABLED", "true")
                .WithReference(postgres)
                .WithReference(n8nDb)
                .WaitFor(postgres)
                .WithHttpHealthCheck("/healthz", 200);
        }
    }
}