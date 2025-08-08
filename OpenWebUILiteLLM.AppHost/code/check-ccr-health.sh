#!/bin/bash

echo "🔍 Checking CCR Service Health..."

# Check if CCR process is running
if pgrep -f "claude-code-router" > /dev/null; then
    echo "✅ CCR process is running"
    CCR_PID=$(pgrep -f "claude-code-router")
    echo "   Process ID: $CCR_PID"
else
    echo "❌ CCR process is not running"
    exit 1
fi

# Check if CCR is listening on port 3456
if netstat -tulpn 2>/dev/null | grep -q ":3456.*LISTEN"; then
    echo "✅ CCR is listening on port 3456"
else
    echo "❌ CCR is not listening on port 3456"
    echo "📊 Current listening ports:"
    netstat -tulpn 2>/dev/null | grep LISTEN
    exit 1
fi

# Check if CCR health endpoint responds
if curl -s -f http://localhost:3456/health > /dev/null 2>&1; then
    echo "✅ CCR health endpoint is responding"
    echo "📋 Health check response:"
    curl -s http://localhost:3456/health
else
    echo "❌ CCR health endpoint is not responding"
    echo "🔍 Trying to connect to CCR API..."
    
    # Try basic connectivity test
    if nc -z localhost 3456 2>/dev/null; then
        echo "✅ Port 3456 is reachable"
        echo "📋 Trying curl with verbose output:"
        curl -v http://localhost:3456/health
    else
        echo "❌ Port 3456 is not reachable"
    fi
    
    exit 1
fi

echo "🎉 CCR service is healthy!"