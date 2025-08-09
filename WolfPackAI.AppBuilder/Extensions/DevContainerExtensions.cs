using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace WolfPackAI.AppBuilder.Extensions
{
    public static class DevContainerExtensions
    {
        public static IResourceBuilder<ContainerResource>? AddDevContainer(
            this IDistributedApplicationBuilder builder,
            int sshPort = 2222,
            int ccrPort = 3456,
            string developerPassword = "devpassword",
            string rootPassword = "supersecurepassword",
            string ccrApiKey = "ccr-dev-key-2024",
            string name = "devcontainer",
            string tag = "24.04")
        {
            try
            {
                return builder.AddContainer(name, "ubuntu", tag)
                    .WithEndpoint(targetPort: 22, port: sshPort, scheme: "tcp", name: "ssh")
                    .WithHttpEndpoint(targetPort: ccrPort, port: ccrPort, name: "ccr-http")
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
                        $"echo 'developer:{developerPassword}' | chpasswd && " +
                        $"echo 'root:{rootPassword}' | chpasswd && " +
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
                        $"  \"APIKEY\": \"{ccrApiKey}\",\n" +
                        "  \"API_TIMEOUT_MS\": 600000,\n" +
                        "  \"HOST\": \"0.0.0.0\",\n" +
                        $"  \"PORT\": {ccrPort},\n" +
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
                        $"for i in {{1..30}}; do if nc -z localhost {ccrPort} 2>/dev/null; then echo 'CCR is ready on port {ccrPort}!'; break; fi; sleep 2; done && " +
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
                return null;
            }
        }
    }
}