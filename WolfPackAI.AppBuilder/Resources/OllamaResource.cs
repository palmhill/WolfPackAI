using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WolfPackAI.AppBuilder.Resources
{
    public class OllamaResource : ContainerResource, IResourceWithConnectionString
    {
        private readonly string _host;
        private readonly string _publicPort;

        public OllamaResource(string name, string modelName, string externalHostIpAddress, string publicPort,
            string? entrypoint = null) : base(name, entrypoint)
        {
            if (string.IsNullOrWhiteSpace(publicPort)) throw new ArgumentNullException(nameof(publicPort));
            if (string.IsNullOrWhiteSpace(modelName)) throw new ArgumentNullException(nameof(modelName));

            ModelName = modelName;
            _host =
                string.IsNullOrWhiteSpace(externalHostIpAddress) ? "localhost" : externalHostIpAddress;
            _publicPort = publicPort;
        }

        private const string OllamaEndpointName = "ollama";

        private EndpointReference? _endpointReference;

        public EndpointReference Endpoint =>
            _endpointReference ??= new EndpointReference(this, OllamaEndpointName);

        public string ModelName { get; }

        public ReferenceExpression ConnectionStringExpression =>
            ReferenceExpression.Create(
                $"http://{_host}:{_publicPort}"
            );
    }
}