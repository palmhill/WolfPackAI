# PrimeGate Troubleshooting Guide

Comprehensive troubleshooting guide for WolfPackAI PrimeGate and Cursor IDE integration.

## Quick Diagnostics

Run these commands to quickly identify issues:

```bash
# 1. Check PrimeGate service status
curl http://localhost:7000/api/status

# 2. Verify model discovery
curl http://localhost:7000/api/llms

# 3. Test LiteLLM connection
curl http://localhost:4000/health

# 4. Test Ollama connection
curl http://localhost:1143/

# 5. View Swagger UI (in browser)
http://localhost:7000/swagger
```

Expected results when everything is working:
- Status returns `"status": "online"`
- LLMs returns at least 2 models
- LiteLLM returns health status
- Ollama returns welcome message
- Swagger UI loads without errors

## Common Issues and Solutions

### Issue 1: PrimeGate Not in Visual Studio Solution

**Symptom:**
- PrimeGate project missing from Solution Explorer
- Cannot build or run PrimeGate from Visual Studio

**Cause:**
- Project not added to WolfPackAI.sln

**Solution:**

```bash
# Navigate to solution directory
cd C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI

# Add project to solution
dotnet sln WolfPackAI.sln add WolfPackAI.PrimeGate\WolfPackAI.PrimeGate.csproj

# Verify it was added
dotnet sln WolfPackAI.sln list
```

Alternative: Use Visual Studio UI:
1. Right-click solution in Solution Explorer
2. "Add" > "Existing Project..."
3. Navigate to `WolfPackAI.PrimeGate\WolfPackAI.PrimeGate.csproj`
4. Click "Open"

**Verification:**
- PrimeGate appears in Solution Explorer
- Solution builds successfully with `dotnet build`

---

### Issue 2: PrimeGate Service Not Starting

**Symptom:**
- `curl http://localhost:7000/api/status` returns connection refused
- Service console shows errors
- Port 7000 not listening

**Diagnostic Steps:**

```bash
# Check if port 7000 is already in use (Windows)
netstat -ano | findstr :7000

# Check if PrimeGate process is running
tasklist | findstr PrimeGate

# View recent PrimeGate logs
dotnet run --project WolfPackAI.PrimeGate
```

**Common Causes and Solutions:**

#### Cause A: Port Conflict
**Solution:** Change port in `appsettings.json`:
```json
{
  "PrimeGate": {
    "Port": 7001  // Changed from 7000
  }
}
```

#### Cause B: Missing Dependencies
**Solution:** Restore NuGet packages:
```bash
dotnet restore WolfPackAI.PrimeGate
dotnet build WolfPackAI.PrimeGate
```

#### Cause C: Configuration Errors
**Check:** Validate `appsettings.json` syntax:
```bash
# Check for JSON syntax errors
type WolfPackAI.PrimeGate\appsettings.json
```

Look for:
- Missing commas
- Unclosed brackets
- Invalid JSON format

**Solution:** Fix JSON syntax errors and restart service.

#### Cause D: ServiceDefaults Project Not Found
**Check:** Verify project reference:
```bash
dotnet list WolfPackAI.PrimeGate reference
```

**Solution:** Add missing reference:
```bash
dotnet add WolfPackAI.PrimeGate reference WolfPackAI.ServiceDefaults
```

---

### Issue 3: Model Discovery Returns Empty/Zero Models

**Symptom:**
```json
{
  "totalModels": 0,
  "sources": {
    "ollama": 0,
    "litellm": 0,
    "cursor-native": 0
  },
  "models": []
}
```

**Diagnostic Steps:**

```bash
# 1. Test Ollama directly
curl http://localhost:1143/api/tags

# 2. Test LiteLLM directly
curl http://localhost:4000/v1/models

# 3. Check PrimeGate configuration
type WolfPackAI.PrimeGate\appsettings.json | findstr "BaseUrl"

# 4. View PrimeGate logs for discovery errors
dotnet run --project WolfPackAI.PrimeGate
```

**Solutions:**

#### Solution A: Ollama Not Running
**Start Ollama:**
```bash
# Via Aspire
dotnet run --project WolfPackAI.AppHost

# Or standalone (if installed)
ollama serve
```

**Verify:**
```bash
curl http://localhost:1143/api/tags
```

#### Solution B: LiteLLM Not Running
**Start LiteLLM:**
```bash
dotnet run --project WolfPackAI.AppHost
```

**Verify:**
```bash
curl http://localhost:4000/health
```

#### Solution C: Wrong Endpoints Configured
**Check endpoints in `appsettings.json`:**
```json
{
  "Endpoints": {
    "LiteLLM": {
      "BaseUrl": "http://localhost:4000"  // Verify port
    },
    "Ollama": {
      "BaseUrl": "http://localhost:1143"  // Verify port
    }
  }
}
```

**Update if needed and restart PrimeGate.**

#### Solution D: Discovery Disabled
**Check discovery configuration:**
```json
{
  "ModelDiscovery": {
    "EnableOllama": true,      // Must be true
    "EnableLiteLLM": true,     // Must be true
    "EnableCursorNative": true // Must be true
  }
}
```

#### Solution E: Health Check Timeout
**Increase timeout in `appsettings.json`:**
```json
{
  "PrimeGate": {
    "HealthCheckTimeoutSeconds": 10  // Increased from 3
  }
}
```

---

### Issue 4: Cursor IDE Cannot See Models

**Symptom:**
- Cursor model dropdown is empty
- Error connecting to LiteLLM endpoint
- Models not appearing in Cursor settings

**Diagnostic Steps:**

```bash
# 1. Verify LiteLLM is accessible
curl http://localhost:4000/v1/models

# 2. Test OpenAI-compatible endpoint
curl http://localhost:4000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer sk-1234" \
  -d '{
    "model": "deepseek-coder-v2:16b",
    "messages": [{"role": "user", "content": "Hello"}]
  }'

# 3. Check PrimeGate model discovery
curl http://localhost:7000/api/llms
```

**Solutions:**

#### Solution A: Cursor Not Configured for LiteLLM
**Configure Cursor:**
1. Open Cursor Settings (`Ctrl+,`)
2. Search for "OpenAI" or "Custom API"
3. Add custom endpoint:
   - Base URL: `http://localhost:4000/v1`
   - API Key: `sk-1234` (or your LiteLLM master key)
4. Save and restart Cursor

#### Solution B: Wrong API Key
**Check LiteLLM master key:**
```bash
# In WolfPackAI.AppHost\appsettings.json
type WolfPackAI.AppHost\appsettings.json | findstr "MasterKey"
```

**Update Cursor with correct key.**

#### Solution C: LiteLLM No Models Configured
**Add models to LiteLLM configuration** in `WolfPackAI.AppHost\litellm-config.yaml`:
```yaml
model_list:
  - model_name: deepseek-coder-v2:16b
    litellm_params:
      model: ollama/deepseek-coder-v2:16b
      api_base: http://ollama:11434
```

**Restart WolfPackAI.AppHost.**

#### Solution D: Network Firewall Blocking
**Check Windows Firewall:**
1. Open Windows Firewall settings
2. Allow inbound connections on port 4000
3. Restart LiteLLM service

---

### Issue 5: Swagger UI Not Loading

**Symptom:**
- `http://localhost:7000/swagger` returns 404
- Swagger page is blank
- No API documentation visible

**Solutions:**

#### Solution A: Swagger Disabled in Configuration
**Enable Swagger in `appsettings.json`:**
```json
{
  "PrimeGate": {
    "EnableSwagger": true
  }
}
```

**Restart PrimeGate.**

#### Solution B: Wrong URL
**Try alternate Swagger URLs:**
- `http://localhost:7000/swagger`
- `http://localhost:7000/swagger/index.html`
- `http://localhost:7000/swagger/v1/swagger.json`

#### Solution C: Development Environment Not Detected
**Force Swagger in all environments:**

Edit `WolfPackAI.PrimeGate\Program.cs` (line 105):
```csharp
// Change this:
if (primeGateSettings.EnableSwagger || app.Environment.IsDevelopment())

// To this (for debugging):
if (true)
```

**Note:** Revert this change after debugging.

---

### Issue 6: CORS Errors in Browser

**Symptom:**
```
Access to fetch at 'http://localhost:7000/api/llms' from origin 'http://localhost:3000'
has been blocked by CORS policy
```

**Solutions:**

#### Solution A: CORS Not Enabled
**Enable CORS in `appsettings.json`:**
```json
{
  "PrimeGate": {
    "EnableCors": true,
    "CorsOrigins": ["*"]  // Allow all origins (development only)
  }
}
```

#### Solution B: Specific Origin Not Allowed
**Add specific origin:**
```json
{
  "PrimeGate": {
    "EnableCors": true,
    "CorsOrigins": [
      "http://localhost:3000",
      "http://localhost:5000",
      "https://cursor.sh"
    ]
  }
}
```

#### Solution C: Preflight Request Failing
**Check browser console for details.**

**Ensure OPTIONS requests are handled:**
- PrimeGate automatically handles OPTIONS via CORS middleware
- Verify CORS is enabled (see Solution A)
- Check for authentication requirements blocking preflight

---

### Issue 7: Execute Endpoint Fails

**Symptom:**
```json
{
  "status": "error",
  "modelUsed": "deepseek-coder-v2:16b",
  "error": "Model execution failed",
  "executionTimeMs": 0
}
```

**Diagnostic Steps:**

```bash
# 1. Test model is available
curl http://localhost:7000/api/llms | grep "deepseek-coder-v2:16b"

# 2. Test Ollama directly
curl http://localhost:1143/api/generate \
  -H "Content-Type: application/json" \
  -d '{
    "model": "deepseek-coder-v2:16b",
    "prompt": "Hello",
    "stream": false
  }'

# 3. Check execution request format
curl -X POST http://localhost:7000/api/execute \
  -H "Content-Type: application/json" \
  -d '{
    "modelId": "deepseek-coder-v2:16b",
    "prompt": "Hello world"
  }'
```

**Solutions:**

#### Solution A: Model Not Available
**Verify model exists:**
```bash
curl http://localhost:1143/api/tags
```

**Pull model if missing:**
```bash
ollama pull deepseek-coder-v2:16b
```

#### Solution B: Invalid Request Format
**Correct request format:**
```json
{
  "modelId": "deepseek-coder-v2:16b",
  "prompt": "Your prompt here",
  "parameters": {
    "temperature": 0.7,
    "max_tokens": 1000
  }
}
```

**Required fields:**
- `modelId` (string) - Must match discovered model ID
- `prompt` (string) - Cannot be empty

**Optional fields:**
- `parameters` (object) - Model-specific parameters

#### Solution C: Timeout Issues
**Increase timeout in `appsettings.json`:**
```json
{
  "Endpoints": {
    "Ollama": {
      "Timeout": 60  // Increased from 30 seconds
    }
  }
}
```

#### Solution D: Model Source Unavailable
**Check which source the model is from:**
```bash
curl http://localhost:7000/api/llms | findstr "deepseek-coder-v2:16b"
```

**Verify that source is healthy:**
- If Ollama: `curl http://localhost:1143/`
- If LiteLLM: `curl http://localhost:4000/health`

---

## Diagnostic Commands Reference

### Service Health Checks

```bash
# PrimeGate status
curl http://localhost:7000/api/status

# LiteLLM health
curl http://localhost:4000/health

# Ollama health
curl http://localhost:1143/

# All services via Aspire Dashboard
# Open browser: http://localhost:15000 (or port shown in console)
```

### Model Discovery

```bash
# PrimeGate model inventory
curl http://localhost:7000/api/llms

# Ollama models
curl http://localhost:1143/api/tags

# LiteLLM models
curl http://localhost:4000/v1/models

# Filter PrimeGate models by source
curl http://localhost:7000/api/llms | findstr "ollama"
curl http://localhost:7000/api/llms | findstr "litellm"
curl http://localhost:7000/api/llms | findstr "cursor-native"
```

### API Testing

```bash
# Get model suggestion
curl -X POST http://localhost:7000/api/suggest \
  -H "Content-Type: application/json" \
  -d '{
    "description": "Refactor authentication code",
    "language": "csharp",
    "taskType": "refactor"
  }'

# Execute task
curl -X POST http://localhost:7000/api/execute \
  -H "Content-Type: application/json" \
  -d '{
    "modelId": "deepseek-coder-v2:16b",
    "prompt": "Explain async/await in C#"
  }'

# Check access policy
curl http://localhost:7000/api/policy
```

### Configuration Validation

```bash
# View PrimeGate configuration
type WolfPackAI.PrimeGate\appsettings.json

# Validate JSON syntax (PowerShell)
Get-Content WolfPackAI.PrimeGate\appsettings.json | ConvertFrom-Json

# Check endpoint configuration
type WolfPackAI.PrimeGate\appsettings.json | findstr "BaseUrl"

# Check enabled features
type WolfPackAI.PrimeGate\appsettings.json | findstr "Enable"
```

### Port and Process Checks

```bash
# Check if port 7000 is in use (Windows)
netstat -ano | findstr :7000

# Check if PrimeGate is running
tasklist | findstr PrimeGate

# Check all relevant ports
netstat -ano | findstr ":7000 :4000 :1143 :8080"

# Kill process on port 7000 (if stuck)
# First find PID from netstat output, then:
taskkill /PID <PID> /F
```

### Log Analysis

```bash
# Run PrimeGate with verbose logging
dotnet run --project WolfPackAI.PrimeGate --verbosity detailed

# Run entire WolfPackAI stack
dotnet run --project WolfPackAI.AppHost

# View Aspire Dashboard for logs
# Check console output for dashboard URL (usually http://localhost:15000)
```

## Advanced Troubleshooting

### Debug PrimeGate in Visual Studio

1. **Set PrimeGate as Startup Project**
   - Right-click WolfPackAI.PrimeGate in Solution Explorer
   - "Set as Startup Project"

2. **Set Breakpoints**
   - Open `Program.cs`
   - Set breakpoint on line 150 (GET /api/llms endpoint)

3. **Run with Debugger**
   - Press F5
   - Service starts with debugger attached

4. **Test Endpoint**
   - Open browser: `http://localhost:7000/api/llms`
   - Breakpoint should hit

5. **Inspect Variables**
   - Check `discoveryService` variable
   - Step through model discovery logic

### Enable Detailed HTTP Logging

Add to `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information",  // Changed from Warning
      "System.Net.Http.HttpClient": "Information"  // Changed from Warning
    }
  }
}
```

Restart PrimeGate to see detailed HTTP request/response logs.

### Test with Postman or Thunder Client

1. **Import OpenAPI Spec:**
   - Get spec: `http://localhost:7000/swagger/v1/swagger.json`
   - Import into Postman or Thunder Client

2. **Test Each Endpoint:**
   - GET /api/status
   - GET /api/llms
   - POST /api/suggest
   - POST /api/execute
   - GET /api/policy

3. **Save Successful Requests:**
   - Create collection for future testing
   - Share with team for consistent testing

### Clear Discovery Cache

PrimeGate caches discovery results for 30 seconds.

**To force refresh:**
1. Restart PrimeGate service
2. Or wait 30 seconds
3. Or reduce cache time in config:
```json
{
  "PrimeGate": {
    "DiscoveryCacheSeconds": 5  // Reduced from 30
  }
}
```

## Getting Help

### Before Asking for Help

1. **Run all quick diagnostics** (top of this document)
2. **Check service logs** for error messages
3. **Verify configuration** matches examples
4. **Test with curl** before testing with Cursor
5. **Review related documentation**

### Information to Provide

When reporting issues, include:

1. **Error Message:**
   ```
   Full error text from console or logs
   ```

2. **Service Status:**
   ```bash
   curl http://localhost:7000/api/status
   curl http://localhost:4000/health
   curl http://localhost:1143/
   ```

3. **Model Discovery:**
   ```bash
   curl http://localhost:7000/api/llms
   ```

4. **Configuration:**
   ```bash
   # Sanitize any API keys before sharing
   type WolfPackAI.PrimeGate\appsettings.json
   ```

5. **Environment:**
   - Windows version
   - .NET version: `dotnet --version`
   - Aspire version: Check WolfPackAI.AppHost.csproj

### Related Documentation

- [CURSOR_INTEGRATION.md](./CURSOR_INTEGRATION.md) - Integration guide
- [CLAUDE.md](../CLAUDE.md) - Complete architecture documentation
- [Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [LiteLLM Documentation](https://docs.litellm.ai/)
- [Ollama Documentation](https://ollama.ai/docs)

### Support Channels

1. **Check Existing Issues:** Review git repository issues
2. **Create New Issue:** Provide all diagnostic information
3. **Ask Team:** Internal developer Slack/Teams channels
4. **Community:** Aspire or LiteLLM community forums

---

## Quick Reference Card

**Service Endpoints:**
- PrimeGate: `http://localhost:7000`
- LiteLLM: `http://localhost:4000`
- Ollama: `http://localhost:1143`
- Swagger: `http://localhost:7000/swagger`

**Key Configuration Files:**
- PrimeGate: `WolfPackAI.PrimeGate\appsettings.json`
- AppHost: `WolfPackAI.AppHost\appsettings.json`
- LiteLLM: `WolfPackAI.AppHost\litellm-config.yaml`

**Essential Commands:**
```bash
# Start all services
dotnet run --project WolfPackAI.AppHost

# Start PrimeGate only
dotnet run --project WolfPackAI.PrimeGate

# Test health
curl http://localhost:7000/api/status

# Discover models
curl http://localhost:7000/api/llms

# Build solution
dotnet build
```

**Common Fixes:**
1. Restart PrimeGate: `Ctrl+C` then rerun
2. Clear cache: Wait 30 seconds or restart
3. Check ports: `netstat -ano | findstr :7000`
4. Verify config: JSON syntax check
5. Check logs: Aspire Dashboard

---

**Last Updated:** 2025-11-01
**PrimeGate Version:** 1.0.0
**Maintainers:** WolfPackAI Team
