#!/bin/bash

# Claude Code Router Startup Script
echo "🚀 Starting Claude Code Router..."

# Set up environment variables
export DEBIAN_FRONTEND=noninteractive
export NODE_ENV=production

# Create CCR configuration directories
mkdir -p ~/.claude-code-router
mkdir -p ~/.claude-code-router/logs

# Copy configuration template if it doesn't exist
if [ ! -f ~/.claude-code-router/config.json ]; then
    echo "📋 Installing CCR configuration..."
    cp /app/code/ccr-config.json ~/.claude-code-router/config.json
    echo "✅ CCR configuration installed"
else
    echo "📋 CCR configuration already exists"
fi

# Set proper permissions
chmod 600 ~/.claude-code-router/config.json
chown -R $(whoami):$(whoami) ~/.claude-code-router

# Check if CCR is already running
if pgrep -f "claude-code-router" > /dev/null; then
    echo "⚠️  CCR is already running"
    ccr status
else
    echo "🔧 Starting CCR service..."
    
    # Start CCR in background
    nohup ccr start > ~/.claude-code-router/logs/ccr.log 2>&1 &
    
    # Wait a moment for startup
    sleep 3
    
    # Check if it started successfully
    if pgrep -f "claude-code-router" > /dev/null; then
        echo "✅ CCR started successfully!"
        echo "🌐 Web UI: http://localhost:3456/ui"
        echo "📊 API: http://localhost:3456/v1/messages"
        ccr status
    else
        echo "❌ Failed to start CCR"
        echo "📋 Check logs:"
        tail -20 ~/.claude-code-router/logs/ccr.log
        exit 1
    fi
fi

# Display connection info
echo ""
echo "🔗 Connection Information:"
echo "  CCR Web UI: http://localhost:3456/ui"
echo "  CCR API: http://localhost:3456/v1/messages" 
echo "  API Key: ccr-dev-key-2024"
echo ""
echo "💡 Usage Examples:"
echo "  ccr status                    # Check CCR status"
echo "  ccr ui                        # Open web UI"
echo "  ccr restart                   # Restart CCR service"
echo "  ccr code 'help me debug'     # Use Claude Code via CCR"
echo ""
echo "🔧 Integration with LiteLLM:"
echo "  LiteLLM API: http://litellm:4000/v1"
echo "  CCR uses LiteLLM as fallback provider"
echo "  Access via reverse proxy: http://localhost:5000/ccr/"
echo ""

# Test connectivity to LiteLLM (if available)
echo "🔍 Testing LiteLLM connectivity..."
if curl -s http://litellm:4000/health > /dev/null 2>&1; then
    echo "✅ LiteLLM is accessible at http://litellm:4000"
else
    echo "⚠️  LiteLLM not accessible - CCR will use external providers only"
fi

echo "🎉 CCR setup complete!"