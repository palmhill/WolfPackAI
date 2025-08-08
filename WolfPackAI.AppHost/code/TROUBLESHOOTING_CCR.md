# CCR Integration Troubleshooting Guide

## Common Issues and Solutions

### 1. 504 Gateway Timeout Error

**Symptoms:**
- Browser shows "504 Gateway Timeout" when accessing `/ccr/ui`
- Error: "Error handling TCP connection" with "no endpoints configured"

**Root Causes:**
- CCR service not running in development container
- Container networking issues
- Reverse proxy pointing to wrong address

**Solutions Applied:**

#### ✅ Fixed Container Service Startup
- Created `container-startup.sh` script that properly starts both SSH and CCR services
- Added health checks and service monitoring
- Configured CCR to run as `developer` user with proper permissions

#### ✅ Fixed Reverse Proxy Configuration  
- Changed CCR cluster destination from `localhost:3456` to `devcontainer:3456`
- Added proper routing for `/ccr/ui`, `/ccr/v1/`, and `/claude-code/` paths
- Added dependency wait for development container

#### ✅ Improved Container Configuration
- Added proper HTTP endpoint exposure on port 3456
- Created health check scripts for service validation
- Implemented automatic CCR configuration setup

### 2. WebSocket Connection Errors

**Symptoms:**
```
The connection to wss://localhost:44340/ReverseProxy/ was interrupted while the page was loading.
dotnet-watch reload socket error.
```

**Explanation:**
- These are ASP.NET Core development-time features (hot reload, browser refresh)
- They don't affect the actual application functionality  
- Safe to ignore in development environment

**Solution:**
- These errors are cosmetic and don't impact CCR functionality
- To disable, set `ASPNETCORE_ENVIRONMENT=Production` if needed
- Or ignore - they don't affect the actual service operations

### 3. Container Network Connectivity

**Symptoms:**
- "not all expected containers are connected to the network, retrying..."
- Missing container connection to Aspire network

**Causes:**
- Container startup timing issues
- Network configuration problems
- Service dependencies not properly configured

**Solutions Applied:**
- Added proper `WaitFor` dependencies in reverse proxy configuration
- Improved container startup script with health checks
- Added network connectivity validation

## Validation Steps

### Step 1: Check Container Status
```bash
# SSH into development container
ssh developer@localhost -p 2222

# Check CCR service status
ccr status

# Check if CCR is running
ps aux | grep claude-code-router

# Check port listening
netstat -tulpn | grep 3456
```

### Step 2: Test CCR Service Directly
```bash
# Inside container - test health endpoint
curl http://localhost:3456/health

# Test CCR API
curl -X POST http://localhost:3456/v1/messages \
  -H "Authorization: Bearer ccr-dev-key-2024" \
  -H "Content-Type: application/json" \
  -d '{"messages":[{"role":"user","content":"Hello"}]}'

# Run health check script
./check-ccr-health.sh
```

### Step 3: Test Reverse Proxy Routing
```bash
# From host machine - test through reverse proxy
curl http://localhost:5000/ccr/health

# Test CCR UI access
curl -I http://localhost:5000/ccr/ui

# Test API routing
curl -X POST http://localhost:5000/ccr/v1/messages \
  -H "Authorization: Bearer ccr-dev-key-2024" \
  -H "Content-Type: application/json" \
  -d '{"messages":[{"role":"user","content":"Hello"}]}'
```

### Step 4: Check Service Dependencies
```bash
# Verify all services are running
curl http://localhost:5000/health
curl http://localhost:5000/litellm/health  
curl http://localhost:5000/n8n/healthz
curl http://localhost:5000/ccr/health
```

## Expected Behavior After Fixes

### ✅ Container Startup
1. Ubuntu container starts with all development tools
2. SSH daemon starts on port 22 (external: 2222)  
3. CCR service starts automatically on port 3456
4. Health checks validate service readiness
5. Configuration files are properly installed

### ✅ Network Connectivity  
1. Reverse proxy can reach `devcontainer:3456`
2. All containers connected to Aspire network
3. Service dependencies resolved correctly
4. Health endpoints responding

### ✅ Web Access
1. Landing page shows CCR service card
2. `/ccr/ui` route works without 504 errors
3. CCR web interface loads correctly
4. API endpoints accessible through reverse proxy

## Manual Recovery Steps

If CCR is still not working after restart:

### 1. SSH into Container and Debug
```bash
ssh developer@localhost -p 2222

# Check service status
ccr status

# View logs
cat ~/.claude-code-router/logs/ccr.log

# Restart CCR manually
ccr restart

# Verify it's working
curl http://localhost:3456/health
```

### 2. Check Configuration
```bash
# Verify config file exists
ls -la ~/.claude-code-router/config.json

# Check config contents  
cat ~/.claude-code-router/config.json

# Restart with debug logging
CCR_DEBUG=true ccr start
```

### 3. Check Network Connectivity
```bash
# From reverse proxy perspective, test if devcontainer is reachable
# (This would be done from reverse proxy container)
curl http://devcontainer:3456/health

# Check port forwarding
netstat -tulpn | grep 3456
```

## Success Indicators

After applying these fixes, you should see:

✅ **Container Logs:**
```
🚀 Starting container services...
🔐 Starting SSH daemon...
🔀 Starting Claude Code Router...
⏳ Waiting for CCR to become ready...
✅ CCR started successfully on port 3456
🎉 Container services started!
```

✅ **Browser Access:**
- `http://localhost:5000/` - Shows CCR service card
- `http://localhost:5000/ccr/ui` - CCR web interface loads
- No 504 Gateway Timeout errors

✅ **API Access:**
- CCR API responds on `/ccr/v1/messages`
- Health checks pass on `/ccr/health`
- Model routing works with `/model` overrides

The WebSocket errors about browser refresh are harmless development-time warnings and don't indicate any problems with the actual CCR functionality.