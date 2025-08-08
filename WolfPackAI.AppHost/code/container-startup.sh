#!/bin/bash

echo "🚀 Starting container services..."

# Set up CCR configuration
if [ ! -f /root/.claude-code-router/config.json ]; then
    echo "📋 Setting up CCR configuration for root..."
    cp /app/code/ccr-config.json /root/.claude-code-router/config.json
    chmod 600 /root/.claude-code-router/config.json
fi

if [ ! -f /home/developer/.claude-code-router/config.json ]; then
    echo "📋 Setting up CCR configuration for developer..."
    cp /app/code/ccr-config.json /home/developer/.claude-code-router/config.json
    chown developer:developer /home/developer/.claude-code-router/config.json
    chmod 600 /home/developer/.claude-code-router/config.json
fi

# Start SSH daemon in background
echo "🔐 Starting SSH daemon..."
/usr/sbin/sshd -D &
SSH_PID=$!

# Wait for SSH to be ready
sleep 2

# Start CCR as developer user
echo "🔀 Starting Claude Code Router..."
export HOME=/home/developer
su - developer -c "cd /app/code && ccr start" &
CCR_PID=$!

# Wait a moment for CCR to start
sleep 5

# Wait for CCR to be ready (up to 30 seconds)
echo "⏳ Waiting for CCR to become ready..."
for i in {1..30}; do
    if curl -s http://localhost:3456/health > /dev/null 2>&1; then
        echo "✅ CCR started successfully on port 3456"
        break
    elif [ $i -eq 30 ]; then
        echo "⚠️ CCR is not responding after 30 seconds, checking status..."
        su - developer -c "ccr status"
        # Try to start CCR manually if it's not running
        if ! pgrep -f "claude-code-router" > /dev/null; then
            echo "🔄 CCR process not found, starting manually..."
            su - developer -c "cd /app/code && ccr start" &
        fi
    else
        echo "⏳ Waiting for CCR... ($i/30)"
        sleep 1
    fi
done

echo "🎉 Container services started!"
echo "📊 Service Status:"
echo "  SSH: PID $SSH_PID"
echo "  CCR: PID $CCR_PID"
echo "  SSH Port: 22"
echo "  CCR Port: 3456"
echo ""

# Function to handle shutdown
cleanup() {
    echo "🛑 Shutting down services..."
    su - developer -c "ccr stop" || true
    kill $SSH_PID 2>/dev/null || true
    exit 0
}

# Set up signal handlers
trap cleanup SIGTERM SIGINT

# Keep the container running and monitor services
while true; do
    # Check if SSH is still running
    if ! kill -0 $SSH_PID 2>/dev/null; then
        echo "❌ SSH daemon died, restarting..."
        /usr/sbin/sshd -D &
        SSH_PID=$!
    fi
    
    # Check if CCR is still responding
    if ! curl -s http://localhost:3456/health > /dev/null 2>&1; then
        echo "⚠️ CCR not responding, checking status..."
        su - developer -c "ccr status"
        
        # Try to restart CCR if it's not running
        if ! pgrep -f "claude-code-router" > /dev/null; then
            echo "🔄 Restarting CCR..."
            su - developer -c "cd /app/code && ccr start" &
            CCR_PID=$!
        fi
    fi
    
    sleep 30
done