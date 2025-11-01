# 🔌 Installing WolfPackAI PrimeGate in Other Projects

**Purpose**: Enable PaiiD, PaπD 2mx, or any other project to access WolfPackAI LLMs
**Time**: < 5 minutes per project
**Safety**: 100% isolated, won't affect WolfPackAI installation

---

## 🎯 Overview

After confirming primegate works in WolfPackAI (terminal + Cursor), you can optionally enable it in other projects:

```
PaiiD/                     ← Enable primegate here (optional)
PaπD 2mx/                  ← Enable primegate here (optional)
WolfPackAI/                ← Already has primegate ✅
  └─ PrimeGate Service    ← Runs from here, shared by all
```

**Key Points:**
- PrimeGate service runs ONCE from WolfPackAI
- Other projects just connect to it (client-only)
- Each project can enable/disable independently
- Zero interference between projects

---

## 📋 Prerequisites

**Before installing in other projects:**

1. ✅ PrimeGate working in WolfPackAI
2. ✅ Tested in terminal (`curl http://localhost:7000/api/status`)
3. ✅ Tested in Cursor (see [🐺 N LLMs] status bar)
4. ✅ Confirmed isolation (PaiiD/PaπD currently unaffected)

---

## 🚀 Installation Steps (Per Project)

### Step 1: Copy Configuration Files

**For PaiiD:**
```powershell
# Navigate to PaiiD
cd C:\Users\SSaint-Cyr\Documents\GitHub\PaiiD

# Copy .cursorrules
Copy-Item ..\WolfPackAI\.cursorrules .\.cursorrules

# Copy Cursor extension directory
Copy-Item -Recurse ..\WolfPackAI\.cursor .\.cursor
```

**For PaπD 2mx:**
```powershell
# Navigate to PaπD 2mx
cd "C:\Users\SSaint-Cyr\Documents\GitHub\PaπD 2mx"

# Copy .cursorrules
Copy-Item ..\WolfPackAI\.cursorrules .\.cursorrules

# Copy Cursor extension directory
Copy-Item -Recurse ..\WolfPackAI\.cursor .\.cursor
```

### Step 2: Update Extension Path Check

**Edit** `.cursor\extensions\wolfpackai-primegate.js` in your project:

**Find this line:**
```javascript
const isWolfPackAI = projectPath.includes("WolfPackAI");
```

**Change to:**
```javascript
// Enable for WolfPackAI, PaiiD, and PaπD 2mx
const isWolfPackAI = projectPath.includes("WolfPackAI") ||
                     projectPath.includes("PaiiD") ||
                     projectPath.includes("PaπD");
```

**Or for ANY project** (universal mode):
```javascript
// Enable for ALL projects
const isWolfPackAI = true;  // PrimeGate active everywhere
```

### Step 3: Test Installation

```powershell
# Make sure WolfPackAI primegate is running
# (From WolfPackAI directory in separate terminal)
cd C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI
dotnet run --project WolfPackAI.PrimeGate

# Open Cursor in your other project
cd C:\Users\SSaint-Cyr\Documents\GitHub\PaiiD  # or PaπD 2mx
cursor .

# Check status bar - should see:
# [🐺 12 LLMs]  ← PrimeGate active!
```

---

## 🔧 Per-Project Customization

### Option A: Full Access (Recommended)
```javascript
// In .cursor/extensions/wolfpackai-primegate.js
// Allow this project to use all WolfPackAI LLMs
const isWolfPackAI = projectPath.includes("PaiiD");
```

Result: PaiiD has access to all 15+ models

### Option B: Restricted Access
**Edit** `.cursorrules` to limit which models are exposed:

```
# In PaiiD/.cursorrules
## Available LLMs (Restricted)

**Only expose local models:**
- deepseek-coder-v2:16b
- cursor-fast
- cursor-smart

**Do NOT expose cloud models** (save costs)
```

### Option C: Conditional Activation
```javascript
// Only activate when specific environment variable set
const isWolfPackAI = process.env.ENABLE_WOLFPACK_AI === "true";
```

Usage:
```powershell
$env:ENABLE_WOLFPACK_AI = "true"
cursor .
```

---

## 🎨 Example: Full PaiiD Setup

### Complete Installation Commands

```powershell
# 1. Navigate to PaiiD
cd C:\Users\SSaint-Cyr\Documents\GitHub\PaiiD

# 2. Copy files
Copy-Item ..\WolfPackAI\.cursorrules .\.cursorrules
Copy-Item -Recurse ..\WolfPackAI\.cursor .\.cursor

# 3. Update path check
$extensionPath = ".\.cursor\extensions\wolfpackai-primegate.js"
$content = Get-Content $extensionPath -Raw
$content = $content -replace 'projectPath\.includes\("WolfPackAI"\)', 'projectPath.includes("WolfPackAI") || projectPath.includes("PaiiD")'
Set-Content $extensionPath $content

# 4. Verify
Write-Host "✅ PrimeGate installed in PaiiD!"
Write-Host "Start WolfPackAI primegate, then open Cursor here."
```

### Test It Works

```powershell
# Terminal 1: Start primegate (from WolfPackAI)
cd ..\WolfPackAI
dotnet run --project WolfPackAI.PrimeGate

# Terminal 2: Open Cursor in PaiiD
cd ..\PaiiD
cursor .

# Look for [🐺 N LLMs] in Cursor status bar
```

---

## 🛡️ Isolation Verification

**After installing in PaiiD, verify isolation:**

```powershell
# Should be TRUE (primegate enabled)
Test-Path "C:\...\PaiiD\.cursorrules"

# Should be TRUE (primegate enabled)
Test-Path "C:\...\PaiiD\.cursor\extensions\wolfpackai-primegate.js"

# Should be TRUE (original still intact)
Test-Path "C:\...\WolfPackAI\.cursorrules"

# Important: Verify files are COPIES, not links
(Get-Item "C:\...\PaiiD\.cursorrules").LinkType  # Should be empty (not a link)
```

**Projects remain independent:**
- Deleting PaiiD primegate files → Doesn't affect WolfPackAI
- Deleting WolfPackAI primegate files → Doesn't affect PaiiD
- Each project can be configured differently

---

## 🚨 Troubleshooting

### Issue: "No [🐺 N LLMs] in PaiiD"

**Diagnosis:**
```powershell
# 1. Check if primegate service running
curl http://localhost:7000/api/status

# 2. Check if extension file exists
Test-Path ".\.cursor\extensions\wolfpackai-primegate.js"

# 3. Check if path check updated
Select-String -Path ".\.cursor\extensions\wolfpackai-primegate.js" -Pattern "PaiiD"
# Should return a match
```

**Fix:**
```powershell
# Re-run Step 2 (update path check)
```

### Issue: "Worried about project contamination"

**Solution:**
- Each project has its OWN copy of files
- Modifications in one project don't affect others
- Can delete from any project without affecting others

**Test:**
```powershell
# Delete from PaiiD
cd PaiiD
Remove-Item -Recurse .cursor
Remove-Item .cursorrules

# Check WolfPackAI still works
cd ..\WolfPackAI
cursor .  # Still sees [🐺 N LLMs] ✅
```

### Issue: "Want different models in different projects"

**Solution:**
Edit `.cursorrules` in each project:

**PaiiD/.cursorrules:**
```
## Available LLMs
- deepseek-coder-v2:16b (local, fast)
- cursor-fast
```

**PaπD 2mx/.cursorrules:**
```
## Available LLMs
- gpt-4-turbo (cloud, quality)
- claude-sonnet-4 (cloud, code)
- cursor-smart
```

Each project sees different models!

---

## 📊 Multi-Project Architecture

```
┌─────────────────────┐
│ WolfPackAI          │
│ ├─ PrimeGate ◄────────── Service runs here (shared)
│ ├─ LiteLLM          │
│ ├─ Ollama           │
│ └─ .cursorrules ✅  │
└─────────────────────┘
         ▲
         │ Port 7000
         │
    ┌────┴────────────────────┐
    │                         │
┌───┴─────┐            ┌──────┴──┐
│ PaiiD   │            │ PaπD 2mx│
│ Connect │            │ Connect │
│ Only ✅ │            │ Only ✅ │
└─────────┘            └─────────┘
```

**Benefits:**
- One primegate service (efficient)
- Multiple projects benefit (shared resource)
- Independent configurations (flexible)
- Easy to enable/disable per project

---

## 🔄 Uninstalling from Other Projects

**To remove primegate from PaiiD (keeps WolfPackAI intact):**

```powershell
cd C:\Users\SSaint-Cyr\Documents\GitHub\PaiiD

# Delete files
Remove-Item -Recurse .cursor
Remove-Item .cursorrules

# Verify
cursor .  # No [🐺 N LLMs] (correct!)
```

**WolfPackAI still works:**
```powershell
cd ..\WolfPackAI
cursor .  # Still see [🐺 N LLMs] ✅
```

---

## ✅ Installation Checklist (Per Project)

**For each project you want to enable:**

- [ ] PrimeGate tested and working in WolfPackAI
- [ ] Copied `.cursorrules` to project directory
- [ ] Copied `.cursor/` directory to project
- [ ] Updated `wolfpackai-primegate.js` path check
- [ ] Tested: Start primegate service
- [ ] Tested: Open Cursor in project
- [ ] Verified: See [🐺 N LLMs] status indicator
- [ ] Tested: Can use WolfPackAI models
- [ ] Confirmed: WolfPackAI still works independently

**All checked?** → Project successfully connected! 🎉

---

## 🎯 Recommended Setup

**Start with:**
1. ✅ WolfPackAI (already has primegate)
2. ✅ Test thoroughly in terminal + Cursor
3. ✅ Install in ONE other project (e.g., PaiiD)
4. ✅ Test that too
5. ✅ Once confident, install in PaπD 2mx and others

**Benefits of gradual rollout:**
- Learn how it works in WolfPackAI first
- See isolation in action (one project, not others)
- Build confidence before wider deployment
- Easy to troubleshoot if issues

---

## 🚀 Advanced: Universal Installation Script

**Create** `install-primegate-everywhere.ps1`:

```powershell
# Install primegate in all projects (use with caution!)
param([string[]]$Projects = @("PaiiD", "PaπD 2mx"))

foreach ($project in $Projects) {
    Write-Host "Installing in $project..."

    $projectPath = "C:\Users\SSaint-Cyr\Documents\GitHub\$project"

    if (Test-Path $projectPath) {
        Copy-Item .cursorrules "$projectPath\.cursorrules" -Force
        Copy-Item -Recurse .cursor "$projectPath\.cursor" -Force

        # Update path check
        $ext = "$projectPath\.cursor\extensions\wolfpackai-primegate.js"
        $content = Get-Content $ext -Raw
        $content = $content -replace 'projectPath\.includes\("WolfPackAI"\)', "true"
        Set-Content $ext $content

        Write-Host "  ✅ $project done"
    } else {
        Write-Host "  ⚠ $project not found"
    }
}
```

---

**Once working in WolfPackAI, you can easily extend to all your projects!** 🔌✨
