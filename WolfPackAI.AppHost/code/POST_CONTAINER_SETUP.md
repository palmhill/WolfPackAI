# Post-Container Setup for CCR Integration

Due to line ending issues with embedded scripts, CCR setup needs to be done after the container starts.

## Quick Setup Steps

### 1. Start the Aspire Application
```bash
dotnet run --project WolfPackAI.AppHost
```

### 2. Wait for Container to Start
Wait for the development container to show as "Running" in the Aspire dashboard.

### 3. SSH into Container and Setup CCR
```bash
# SSH into the development container
ssh developer@localhost -p 2222
# Password: devpassword

# Once inside the container, run the setup script
cd /app/code
chmod +x setup-ccr-in-container.sh
sudo ./setup-ccr-in-container.sh
```

### 4. Verify CCR is Working
```bash
# Check if CCR process is running
ps aux | grep claude-code-router

# Check if port 3456 is listening  
netstat -tulpn | grep 3456

# Test CCR status
ccr status

# Test basic functionality
ccr code "Hello, test CCR integration"
```

### 5. Access CCR Web UI
Once CCR is running inside the container:
- **Direct Access**: http://localhost:3456/ui (from within container)  
- **Via Reverse Proxy**: http://localhost:5000/ccr/ui (from host machine)

## Troubleshooting

### If CCR Fails to Start
```bash
# Check Node.js and npm are installed
node --version
npm --version

# Manually install CCR if needed
npm install -g @anthropic-ai/claude-code @musistudio/claude-code-router

# Check CCR installation
ccr --version

# Start CCR manually with debug
CCR_DEBUG=true ccr start
```

### If Setup Script Fails
Run commands manually inside the container:

```bash
# Install packages
sudo apt-get update
sudo apt-get install -y software-properties-common ca-certificates curl gnupg

# Add .NET repository
sudo add-apt-repository ppa:dotnet/backports -y

# Add Node.js repository
curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | sudo gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg
echo 'deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_22.x nodistro main' | sudo tee /etc/apt/sources.list.d/nodesource.list

# Install .NET and Node.js
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0 nodejs

# Install CCR
npm install -g @anthropic-ai/claude-code @musistudio/claude-code-router

# Create CCR config directory
mkdir -p ~/.claude-code-router

# Create basic config
cat > ~/.claude-code-router/config.json << 'EOF'
{
  "APIKEY": "ccr-dev-key-2024",
  "API_TIMEOUT_MS": 600000,
  "HOST": "0.0.0.0",
  "PORT": 3456,
  "ENABLE_ROUTER": true,
  "providers": [
    {
      "name": "litellm-fallback", 
      "api_base_url": "http://litellm:4000/v1",
      "api_key": "your-litellm-key",
      "models": ["claude-3-5-sonnet"],
      "transformers": ["Anthropic"]
    }
  ],
  "router": {
    "default": "litellm-fallback,claude-3-5-sonnet"
  }
}
EOF

# Start CCR
ccr start
```

## Expected Results

After successful setup:

✅ **Container Access**: SSH works on `ssh developer@localhost -p 2222`
✅ **CCR Service**: Running on port 3456 inside container
✅ **Web Access**: CCR UI accessible at http://localhost:5000/ccr/ui  
✅ **API Access**: CCR API available at http://localhost:5000/ccr/v1/
✅ **Integration**: CCR can route to LiteLLM as fallback provider

## Alternative: Use Existing Services

If CCR setup continues to have issues, the system still provides full functionality through:

- **OpenWebUI**: http://localhost:5000/chat - Full chat interface with LiteLLM
- **LiteLLM Direct**: http://localhost:5000/litellm/ - Direct API access
- **n8n Workflows**: http://localhost:5000/n8n/ - Automation platform

The development container still provides all programming languages and tools (C/C++, .NET, Node.js, Python) for development work without CCR.