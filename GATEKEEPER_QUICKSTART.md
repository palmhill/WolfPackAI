# 🐺 WolfPackAI Gatekeeper - Quick Start Guide

**Version**: 1.0.0
**Status**: ✅ BUILD SUCCESS (0 errors, 0 warnings)
**Installation**: 3 simple commands
**Time to Working**: < 2 minutes

---

## 🚀 Installation (3 Commands)

Open PowerShell in the WolfPackAI directory and run:

```powershell
# 1. Navigate to WolfPackAI (if not already there)
cd C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI

# 2. Run the injection script
.\scripts\inject-gatekeeper.ps1

# 3. Done! You'll see success message
```

**Expected Output:**
```
🐺 WolfPackAI Gatekeeper - Seamless Injection
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[1/7] Verifying location...
   ✓ Location confirmed
[2/7] Checking isolation...
   ✓ Isolation confirmed
[3/7] Building Gatekeeper service...
   ✓ Build successful
...
✅ Gatekeeper injected successfully!
```

---

## 🎯 Testing in Terminal (2 Steps)

### Step 1: Start WolfPackAI Services

**Terminal 1:**
```powershell
# Start the main WolfPackAI platform
dotnet run --project WolfPackAI.AppHost
```

Wait for:
```
✅ All services started
🌐 Dashboard: http://localhost
📊 Aspire Dashboard: http://localhost:15021
```

### Step 2: Start Gatekeeper

**Terminal 2** (new PowerShell window):
```powershell
# Start the gatekeeper service
dotnet run --project WolfPackAI.Gatekeeper
```

Wait for:
```
🐺 WolfPackAI Gatekeeper starting...
   Role: Compliant LLM Exposure
   Authority: Orchestrator (Full Control)
   Mode: Seamless Injection
   Listening on: http://localhost:7000
```

---

## 🧪 Verify It's Working (Terminal Tests)

### Test 1: Check Gatekeeper Status
```powershell
curl http://localhost:7000/api/status
```

**Expected Response:**
```json
{
  "service": "WolfPackAI.Gatekeeper",
  "version": "1.0.0",
  "status": "online",
  "role": "compliant-expose-all",
  "orchestrator_authority": "full",
  "timestamp": "2025-10-31T..."
}
```

### Test 2: List Available LLMs
```powershell
curl http://localhost:7000/api/llms
```

**Expected Response:**
```json
{
  "TotalModels": 12,
  "Models": [
    {
      "Name": "deepseek-coder-v2:16b",
      "Source": "ollama",
      "Status": "online",
      "Type": "local",
      "Capabilities": ["code", "chat", "local", "fast"]
    },
    {
      "Name": "gpt-4-turbo",
      "Source": "litellm",
      "Status": "online",
      "Type": "cloud",
      "Capabilities": ["code", "chat", "complex", "quality"],
      "Cost": 0.01
    },
    ...
  ]
}
```

### Test 3: Get Access Policy
```powershell
curl http://localhost:7000/api/policy
```

**Expected Response:**
```json
{
  "token_limit": 2147483647,
  "requests_per_minute": 2147483647,
  "concurrent_models": 2147483647,
  "cost_limit": 1.7976931348623157e+308,
  "restrictions": [],
  "note": "Orchestrator has unlimited access to all resources"
}
```

### Test 4: Ask for Model Suggestion
```powershell
curl -X POST http://localhost:7000/api/suggest `
  -H "Content-Type: application/json" `
  -d '{"Task":"refactor code","Context":"optimize performance"}'
```

**Expected Response:**
```json
{
  "suggestion": {
    "ModelName": "claude-sonnet-4",
    "Reason": "Best for code structure",
    "Confidence": 0.9
  },
  "note": "This is a suggestion only. Orchestrator has final decision.",
  "all_available": ["deepseek-coder-v2:16b", "gpt-4-turbo", ...]
}
```

---

## 🎨 Testing in Cursor (2 Steps)

### Step 1: Open Cursor in WolfPackAI Directory

```powershell
# If Cursor is installed, you can open it via command line
cursor .
# Or manually: File → Open Folder → WolfPackAI
```

### Step 2: Verify Gatekeeper is Active

**Look for status bar** (bottom of Cursor window):
```
[🐺 12 LLMs]  ← Should appear if gatekeeper running
```

**Click on it** to see available models:
```
WolfPackAI Gatekeeper
├─ Status: Online
├─ Models: 12 available
├─ Orchestrator: Full Control
└─ Restrictions: None

Available Models:
  ✅ deepseek-coder-v2:16b (local, fast)
  ✅ gpt-4-turbo (cloud, quality)
  ✅ claude-sonnet-4 (cloud, code)
  ✅ cursor-fast (native)
  ✅ cursor-smart (native)
  ... (and 7 more)
```

### Step 3: Test Orchestrator Control

**In Cursor, open command palette** (Ctrl+Shift+P):
- Type "WolfPackAI"
- Should see: "WolfPackAI: Show LLM Inventory"

**Click it** → Should show complete list of available LLMs

---

## 🔧 Troubleshooting

### Issue: "Port 7000 already in use"
**Solution:**
```powershell
# Check what's using port 7000
netstat -ano | findstr :7000

# Kill the process (replace PID with actual process ID)
taskkill /PID <PID> /F

# Restart gatekeeper
dotnet run --project WolfPackAI.Gatekeeper
```

### Issue: "Gatekeeper returns 0 models"
**Diagnosis:**
- Ollama not running
- LiteLLM not running

**Solution:**
```powershell
# Make sure WolfPackAI services are running
dotnet run --project WolfPackAI.AppHost

# Wait 30 seconds for all services to start
# Then check again:
curl http://localhost:7000/api/llms
```

### Issue: "Cursor doesn't show [🐺 N LLMs]"
**Diagnosis:**
- Gatekeeper not running
- Not in WolfPackAI directory (isolation working correctly!)

**Solution:**
```powershell
# 1. Verify you're in WolfPackAI directory
Get-Location  # Should show ...\WolfPackAI

# 2. Verify gatekeeper is running
curl http://localhost:7000/api/status

# 3. Restart Cursor
```

### Issue: "Worried about affecting other projects"
**Solution:**
- Read [GATEKEEPER_ISOLATION.md](GATEKEEPER_ISOLATION.md)
- Gatekeeper is 100% isolated to WolfPackAI directory
- Test: Open PaiiD in Cursor → No [🐺 N LLMs] indicator (correct!)

---

## 📊 What You Get

**Before Gatekeeper:**
```
Cursor Models:
  - cursor-fast
  - cursor-smart

Total: 2 models
```

**After Gatekeeper:**
```
Cursor Models:
  - cursor-fast (native)
  - cursor-smart (native)
  - deepseek-coder-v2:16b (WolfPackAI/Ollama)
  - qwen3:0.6b (WolfPackAI/Ollama)
  - llama3.1:8b (WolfPackAI/Ollama)
  - codellama:13b (WolfPackAI/Ollama)
  - gpt-4-turbo (WolfPackAI/LiteLLM)
  - gpt-4 (WolfPackAI/LiteLLM)
  - gpt-3.5-turbo (WolfPackAI/LiteLLM)
  - claude-opus-4 (WolfPackAI/LiteLLM)
  - claude-sonnet-4 (WolfPackAI/LiteLLM)
  - claude-haiku-4 (WolfPackAI/LiteLLM)
  - gemini-pro (WolfPackAI/LiteLLM)

Total: 15 models (13 added via WolfPackAI!)
```

---

## 🎯 Next: Using in Other Projects

Once you confirm gatekeeper works here, see:
**[GATEKEEPER_OTHER_PROJECTS.md](GATEKEEPER_OTHER_PROJECTS.md)** (coming next)

For installing in PaiiD, PaπD 2mx, or any other project.

---

## ✅ Success Checklist

- [ ] Ran `.\scripts\inject-gatekeeper.ps1` → Success
- [ ] Started WolfPackAI → Services running
- [ ] Started Gatekeeper → Listening on port 7000
- [ ] Tested `/api/status` → Returns "online"
- [ ] Tested `/api/llms` → Returns list of models
- [ ] Tested `/api/policy` → Shows unlimited access
- [ ] Opened Cursor in WolfPackAI → See [🐺 N LLMs]
- [ ] Verified isolation → PaiiD/other projects unaffected

**All checked?** → Gatekeeper is working perfectly! 🎉

---

## 🚀 Power User Tips

### Auto-Start Gatekeeper (Optional)
Create a batch file `start-wolfpackai-full.bat`:
```batch
@echo off
start "WolfPackAI Services" dotnet run --project WolfPackAI.AppHost
timeout /t 10
start "Gatekeeper" dotnet run --project WolfPackAI.Gatekeeper
echo.
echo ✅ WolfPackAI + Gatekeeper started!
echo.
pause
```

Double-click to start both services at once!

### View Swagger API Docs
```
http://localhost:7000/swagger
```
Interactive API documentation and testing interface

### Monitor LLM Availability
```powershell
# Refresh every 5 seconds
while($true) {
  cls
  curl http://localhost:7000/api/llms | ConvertFrom-Json |
    Select-Object -ExpandProperty Models |
    Format-Table Name, Source, Status, Type
  Start-Sleep 5
}
```

---

**You're ready to supercharge your coding with 15+ LLMs!** 🐺✨
