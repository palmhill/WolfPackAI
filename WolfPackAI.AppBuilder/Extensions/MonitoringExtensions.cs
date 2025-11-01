using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace WolfPackAI.AppBuilder.Extensions;

/// <summary>
/// Extensions for adding monitoring services to WolfPackAI
/// </summary>
public static class MonitoringExtensions
{
    /// <summary>
    /// Adds Prometheus monitoring to the application
    /// </summary>
    public static IResourceBuilder<ContainerResource> AddPrometheus(
        this IDistributedApplicationBuilder builder,
        int port = 9090,
        string name = "prometheus",
        string tag = "latest")
    {
        return builder.AddContainer(name, "prom/prometheus", tag)
            .WithHttpEndpoint(port: port, targetPort: 9090, name: "http")
            .WithBindMount("./monitoring/prometheus.yml", "/etc/prometheus/prometheus.yml")
            .WithVolume("prometheus-data", "/prometheus")
            .WithArgs("--config.file=/etc/prometheus/prometheus.yml",
                     "--storage.tsdb.path=/prometheus",
                     "--web.console.libraries=/usr/share/prometheus/console_libraries",
                     "--web.console.templates=/usr/share/prometheus/consoles")
            .WithHttpHealthCheck("/", 200);
    }

    /// <summary>
    /// Adds Grafana dashboard to the application
    /// </summary>
    public static IResourceBuilder<ContainerResource> AddGrafana(
        this IDistributedApplicationBuilder builder,
        int port = 3000,
        string adminUser = "admin",
        string adminPassword = "admin",
        string name = "grafana",
        string tag = "latest")
    {
        return builder.AddContainer(name, "grafana/grafana", tag)
            .WithHttpEndpoint(port: port, targetPort: 3000, name: "http")
            .WithEnvironment("GF_SECURITY_ADMIN_USER", adminUser)
            .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", adminPassword)
            .WithEnvironment("GF_INSTALL_PLUGINS", "grafana-clock-panel,grafana-simple-json-datasource")
            .WithVolume("grafana-data", "/var/lib/grafana")
            .WithBindMount("./monitoring/grafana/dashboards", "/etc/grafana/provisioning/dashboards")
            .WithBindMount("./monitoring/grafana/datasources", "/etc/grafana/provisioning/datasources")
            .WithHttpHealthCheck("/api/health", 200);
    }

    /// <summary>
    /// Adds Jaeger for distributed tracing
    /// </summary>
    public static IResourceBuilder<ContainerResource> AddJaeger(
        this IDistributedApplicationBuilder builder,
        int uiPort = 16686,
        int collectorPort = 14268,
        string name = "jaeger",
        string tag = "latest")
    {
        return builder.AddContainer(name, "jaegertracing/all-in-one", tag)
            .WithHttpEndpoint(port: uiPort, targetPort: 16686, name: "ui")
            .WithHttpEndpoint(port: collectorPort, targetPort: 14268, name: "collector")
            .WithEnvironment("COLLECTOR_OTLP_ENABLED", "true")
            .WithVolume("jaeger-data", "/badger")
            .WithHttpHealthCheck("/", 200);
    }
}
