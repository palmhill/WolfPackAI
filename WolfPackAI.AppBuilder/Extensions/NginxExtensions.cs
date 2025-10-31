using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using WolfPackAI.AppBuilder.Configuration;

namespace WolfPackAI.AppBuilder.Extensions;

public static class NginxExtensions
{
    /// <summary>
    /// Adds an nginx reverse proxy with certbot SSL integration for LiteLLM
    /// </summary>
    public static (IResourceBuilder<ContainerResource> nginx, IResourceBuilder<ContainerResource> certbot) AddNginxWithSSL(
        this IDistributedApplicationBuilder builder,
        LiteLLMSSLConfig sslConfig,
        IResourceBuilder<ContainerResource> litellm,
        string nginxConfigPath = "./nginx-litellm.conf",
        string name = "nginx-litellm",
        string nginxImage = "nginx:alpine",
        string certbotImage = "certbot/certbot")
    {
        // Validate SSL configuration
        sslConfig.Validate();
        
        // Create volumes for certificates and nginx config
        var certsVolume = "litellm-certs";
        var webrootVolume = "certbot-webroot";
        
        // Get LiteLLM endpoint for proxy pass
        var litellmEndpoint = litellm.GetEndpoint("http");

        // Create nginx container with config mount
        // Mount an init script into docker-entrypoint.d to generate a temp cert if missing
        var nginx = builder.AddContainer(name, nginxImage)
            .WithHttpEndpoint(port: sslConfig.HttpPort, targetPort: 80, name: "http")
            .WithHttpEndpoint(port: sslConfig.HttpsPort, targetPort: 443, name: "https")
            .WithBindMount(nginxConfigPath, "/etc/nginx/conf.d/default.conf")
            .WithVolume(certsVolume, "/etc/letsencrypt")
            .WithVolume(webrootVolume, "/var/www/certbot")
            .WithBindMount("./nginx-cert-init.sh", "/docker-entrypoint.d/40-generate-temp-cert.sh")
            .WithEnvironment("DOMAIN", sslConfig.Domain);
            
        
        // Create certbot container for SSL certificate management
        // First run: Obtain initial certificate
        // Note: Certbot will need nginx running for webroot validation
        var certbotArgs = sslConfig.UseStaging 
            ? new[] { "certonly", "--webroot", "--webroot-path=/var/www/certbot", 
                     "--email", sslConfig.Email, "--agree-tos", "--no-eff-email", 
                     "--staging", "-d", sslConfig.Domain, "--non-interactive" }
            : new[] { "certonly", "--webroot", "--webroot-path=/var/www/certbot", 
                     "--email", sslConfig.Email, "--agree-tos", "--no-eff-email", 
                     "-d", sslConfig.Domain, "--non-interactive" };
        
        var certbot = builder.AddContainer($"{name}-certbot", certbotImage)
            .WithVolume(certsVolume, "/etc/letsencrypt")
            .WithVolume(webrootVolume, "/var/www/certbot")
            .WithArgs(certbotArgs)
            .WaitFor(nginx);
        
        return (nginx, certbot);
    }
}

