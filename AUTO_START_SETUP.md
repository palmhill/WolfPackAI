# 🚀 WolfPackAI Auto-Start Setup

Make WolfPackAI + primegate start automatically when Windows boots.

---

## 🎯 Quick Start (Manual Start)

**Just double-click:**
```
start-wolfpack-full.bat
```

This will:
1. Check if Docker is running (start it if needed)
2. Start WolfPackAI services
3. Start primegate
4. Open in separate windows so you can see logs

**Then open Cursor in WolfPackAI directory and you're ready!**

---

## ⚡ Auto-Start on Windows Boot (Set It and Forget It)

### Option 1: Windows Startup Folder (Recommended)

**One-time setup:**

1. **Press `Win+R`**, type: `shell:startup`, press Enter
2. **Right-click** in the Startup folder → New → Shortcut
3. **Browse to:** `C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI\start-wolfpack-silent.bat`
4. **Name it:** "WolfPackAI Auto-Start"
5. **Click Finish**

**That's it!** Next time you boot Windows:
- Docker Desktop starts automatically
- WolfPackAI services start automatically (minimized)
- primegate starts automatically (minimized)
- All models ready when you open Cursor!

### Option 2: Task Scheduler (More Control)

**For delayed start or specific conditions:**

1. **Open Task Scheduler** (Win+R → `taskschd.msc`)
2. **Create Basic Task**
   - Name: "WolfPackAI Auto-Start"
   - Trigger: "When I log on"
   - Action: "Start a program"
   - Program: `C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI\start-wolfpack-silent.bat`
   - Settings:
     - ✅ Run whether user is logged on or not
     - ✅ Run with highest privileges
     - Delay: 30 seconds (optional, lets Windows fully boot first)

---

## 🎨 Using Auto Mode in Cursor

### How It Works with primegate

**When you select "Auto" in Cursor:**

1. **primegate is already running** (started automatically)
2. **Cursor detects all available models** (15+ models)
3. **Orchestrator (Auto mode) sees all options:**
   - cursor-fast (native)
   - cursor-smart (native)
   - deepseek-coder-v2 (WolfPackAI/Ollama) ← Fast local model
   - gpt-4-turbo (WolfPackAI/LiteLLM) ← Complex reasoning
   - claude-sonnet-4 (WolfPackAI/LiteLLM) ← Best for code
   - ... and 10+ more

4. **Auto mode intelligently picks models** based on task:
   ```
   Task: "Build a REST API with tests"

   Auto Mode Decision Tree:
   ├─ Planning architecture → gpt-4-turbo
   ├─ Writing endpoints → claude-sonnet-4
   ├─ Generating tests → deepseek-coder-v2 (fast!)
   └─ Documentation → gpt-3.5-turbo (cost-effective)
   ```

### You Don't Need to Do Anything!

**With auto-start enabled:**

1. **Boot Windows** → Services start automatically (in background)
2. **Open Cursor** in WolfPackAI directory
3. **Select "Auto" mode** (Ctrl+Shift+P → "Cursor Agent")
4. **Give it a task** → Auto mode picks best models automatically
5. **Done!** 🎉

**No manual model selection needed!** Auto mode + primegate = Fully automated optimal model routing.

---

## 🔍 How Auto Mode Picks Models

**Auto mode uses the primegate's `/api/suggest` endpoint:**

```
Your task: "Refactor this authentication code"

Auto mode asks primegate:
POST /api/suggest
{
  "task": "refactor code",
  "context": "authentication, security"
}

primegate suggests:
{
  "suggestion": "claude-sonnet-4",
  "reason": "Best for code structure and security review",
  "confidence": 0.9
}

Auto mode decides:
"I agree, using claude-sonnet-4 for this task"
```

**But remember:** Orchestrator (Auto mode) has FINAL decision. primegate only suggests!

---

## ✅ Verification

**After auto-start is set up, reboot Windows and check:**

1. **Wait 2 minutes** after Windows boots
2. **Open Task Manager** (Ctrl+Shift+Esc)
3. **Look for these processes:**
   - ✅ Docker Desktop.exe
   - ✅ dotnet.exe (WolfPackAI.AppHost)
   - ✅ dotnet.exe (WolfPackAI.primegate)

4. **Test primegate:**
   ```powershell
   curl http://localhost:7000/api/status
   ```
   Should return: `{"status": "online"}`

5. **Open Cursor in WolfPackAI directory**
6. **Check status bar** → Should see: `[🐺 N LLMs]`

**If all ✅ → You're fully automated!**

---

## 🛑 Stopping Services (If Needed)

**To stop all services:**

```powershell
# Stop primegate
taskkill /FI "WINDOWTITLE eq WolfPackAI primegate*" /F

# Stop WolfPackAI Services
taskkill /FI "WINDOWTITLE eq WolfPackAI Services*" /F

# Stop Docker Desktop (optional)
# Just close Docker Desktop from system tray
```

Or create `stop-wolfpack.bat`:
```batch
@echo off
echo Stopping WolfPackAI services...
taskkill /FI "WINDOWTITLE eq WolfPackAI primegate*" /F
taskkill /FI "WINDOWTITLE eq WolfPackAI Services*" /F
echo Done!
pause
```

---

## 💡 Pro Tips

### Resource Management
- **Auto-start uses ~4GB RAM** when all services running
- **If you need resources for other tasks:** Just close the terminal windows
- **Graceful degradation:** Cursor still works with native models if services stop

### Faster Startup
- **Use SSD** for WolfPackAI directory (faster container startup)
- **Docker Desktop settings:** Enable "Start Docker Desktop when you log in"
- **Delay Task Scheduler start by 30-60 seconds** to let Windows boot fully

### Monitoring
- **Check Aspire Dashboard** for service health: http://localhost:17064
- **Check primegate API docs:** http://localhost:7000/swagger
- **Watch resource usage** in Task Manager

---

## 🎯 Workflow Examples

### Scenario 1: Daily Coding
```
Morning:
1. Boot Windows → Auto-start handles everything
2. Make coffee ☕ (services start in background)
3. Open Cursor in WolfPackAI
4. Select Auto mode
5. Start coding with 15+ AI models!
```

### Scenario 2: Quick Fix (Services Offline)
```
Need quick fix, services not running:
1. Open Cursor (works with native models)
2. Use cursor-fast/cursor-smart
3. No waiting, no setup!
```

### Scenario 3: Complex Project
```
Big refactoring task:
1. Services already running (auto-start)
2. Open Cursor, select Auto mode
3. Give complex task
4. Auto mode uses multiple models:
   - Architecture: gpt-4-turbo
   - Coding: claude-sonnet-4
   - Tests: deepseek-coder-v2
5. Optimal results automatically!
```

---

## ✅ Summary

**To make it fully automatic:**
1. ✅ Add `start-wolfpack-silent.bat` to Windows Startup folder
2. ✅ Reboot Windows to test
3. ✅ Open Cursor in WolfPackAI directory
4. ✅ Select Auto mode
5. ✅ **DONE! Just work, no setup!**

**From now on:**
- Boot Windows → Everything starts
- Open Cursor → All models ready
- Select Auto → Optimal model routing
- **Zero manual steps!** 🚀

---

**Your workflow is now:**
1. Boot computer
2. Open Cursor
3. Code with superpowers
4. That's it! 🎉
