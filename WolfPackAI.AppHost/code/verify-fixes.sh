#!/bin/bash

echo "🔍 Verifying CCR Integration Fixes..."
echo "=================================="

# Test 1: Check if we can SSH into the container
echo "1. Testing SSH access to development container..."
timeout 5 ssh -o BatchMode=yes -o ConnectTimeout=3 developer@localhost -p 2222 'echo "SSH access successful"' 2>/dev/null
if [ $? -eq 0 ]; then
    echo "   ✅ SSH access working"
else
    echo "   ❌ SSH access failed"
fi

# Test 2: Check if CCR service is accessible
echo "2. Testing CCR service accessibility..."
if curl -s http://localhost:3456/health > /dev/null 2>&1; then
    echo "   ✅ CCR direct access working"
elif nc -z localhost 3456 2>/dev/null; then
    echo "   ⚠️  CCR port accessible but health check failed"
else
    echo "   ❌ CCR service not accessible"
fi

# Test 3: Check reverse proxy routing to CCR
echo "3. Testing reverse proxy routing to CCR..."
if curl -s -I http://localhost:5000/ccr/ui | head -n 1 | grep -q "200\|302\|404"; then
    echo "   ✅ CCR reverse proxy routing working"
elif curl -s -I http://localhost:5000/ccr/ui | head -n 1 | grep -q "502"; then
    echo "   ❌ 502 Bad Gateway - CCR service not reachable"
elif curl -s -I http://localhost:5000/ccr/ui | head -n 1 | grep -q "504"; then
    echo "   ❌ 504 Gateway Timeout - CCR service not responding"
else
    echo "   ⚠️  Unexpected response from reverse proxy"
fi

# Test 4: Check if all expected containers are running
echo "4. Checking container connectivity..."
echo "   Aspire services:"
services=("http://localhost:5000/" "http://localhost:5000/chat" "http://localhost:5000/litellm/" "http://localhost:5000/n8n/")
for service in "${services[@]}"; do
    if curl -s -I "$service" | head -n 1 | grep -qE "200|302|401"; then
        echo "   ✅ $(basename $service) accessible"
    else
        echo "   ❌ $(basename $service) not accessible"
    fi
done

echo ""
echo "🔧 If issues persist, check:"
echo "   - Container logs in Docker Desktop"
echo "   - Aspire dashboard for service health"
echo "   - SSH into container: ssh developer@localhost -p 2222"
echo "   - Check CCR status: ccr status"
echo ""

# SSH into container and run internal checks
echo "5. Running internal container checks..."
timeout 10 ssh -o BatchMode=yes -o ConnectTimeout=3 developer@localhost -p 2222 '
echo "   Internal container verification:"
if pgrep -f "claude-code-router" > /dev/null; then
    echo "   ✅ CCR process running"
else
    echo "   ❌ CCR process not found"
fi

if netstat -tulpn 2>/dev/null | grep -q ":3456.*LISTEN"; then
    echo "   ✅ Port 3456 listening"
else
    echo "   ❌ Port 3456 not listening"
fi

if curl -s http://localhost:3456/health > /dev/null 2>&1; then
    echo "   ✅ CCR health endpoint responding"
else
    echo "   ❌ CCR health endpoint not responding"
fi
' 2>/dev/null

echo ""
echo "🎉 Verification complete!"