using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using WolfPackAI.AppBuilder.Resources;

namespace WolfPackAI.AppBuilder.Extensions
{
    public static class PrimeGateExtensions
    {
        public static IResourceBuilder<ProjectResource> AddPrimeGate(
            this IDistributedApplicationBuilder builder,
            IResourceBuilder<ContainerResource> litellm,
            IResourceBuilder<OllamaResource> ollama,
            string name = "primegate",
            string projectPath = "../WolfPackAI.PrimeGate/WolfPackAI.PrimeGate.csproj")
        {
            var primegate = builder.AddProject(name, projectPath)
                .WithHttpEndpoint(targetPort: 7000, name: "http")
                .WithEnvironment("ASPNETCORE_URLS", "http://+:7000");

            // Add references without WithReference since they're containers
            primegate.WaitFor(litellm);
            primegate.WaitFor(ollama);

            return primegate;
        }
    }
}
