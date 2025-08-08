#!/bin/bash
set -e

echo "Setting up CCR in development container..."

# Install required packages
apt-get update
apt-get install -y software-properties-common ca-certificates curl gnupg \
    git vim nano make gcc g++ cmake libtool autoconf automake \
    libc6-dev libstdc++6 python3-pip python3-venv sudo \
    build-essential netcat-openbsd

# Add .NET repository
add-apt-repository ppa:dotnet/backports -y

# Add Node.js repository
mkdir -p /etc/apt/keyrings
curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg
echo 'deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_22.x nodistro main' > /etc/apt/sources.list.d/nodesource.list

# Install .NET and Node.js
apt-get update
apt-get install -y dotnet-sdk-9.0 nodejs

# Install CCR packages
npm install -g @anthropic-ai/claude-code @musistudio/claude-code-router

# Setup CCR directories
mkdir -p /root/.claude-code-router /home/developer/.claude-code-router
chown -R developer:developer /home/developer/.claude-code-router

# Create CCR config
cat > /root/.claude-code-router/config.json << 'EOF'
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

# Copy config for developer user
cp /root/.claude-code-router/config.json /home/developer/.claude-code-router/config.json
chown developer:developer /home/developer/.claude-code-router/config.json

# Start CCR as developer user
su - developer -c 'export HOME=/home/developer && cd ~ && nohup ccr start > ccr.log 2>&1 &'

# Wait for CCR to be ready
echo "Waiting for CCR to start..."
for i in {1..30}; do
    if nc -z localhost 3456 2>/dev/null; then
        echo "CCR is ready on port 3456!"
        break
    fi
    sleep 2
done

echo "CCR setup complete!"