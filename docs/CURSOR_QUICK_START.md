# Cursor + WolfPackAI - Quick Start Guide

**TL;DR: Configuration is DONE. Just restart Cursor!**

---

## Current Status

✅ **Cursor settings configured**
✅ **Ollama models accessible** (deepseek-coder-v2:16b)
✅ **LiteLLM proxy working** (with auth sk-dev-1234)
✅ **Workspace settings created**
✅ **All endpoints verified**

---

## What You Need to Do NOW

### Step 1: Restart Cursor IDE
1. **Close** all Cursor windows completely
2. **Relaunch** Cursor IDE
3. **Open** the WolfPackAI project

### Step 2: Verify It Works
1. Open Cursor Chat (Ctrl+L or Cmd+L)
2. Check model dropdown shows: `deepseek-coder-v2:16b`
3. Send test message: "Hello, are you working?"
4. Should get fast response from local model

---

## Configuration Summary

### What's Configured

**Global Settings** (All Projects):
- File: `C:\Users\SSaint-Cyr\AppData\Roaming\Cursor\User\settings.json`
- Uses: LiteLLM proxy (http://localhost:4000)
- Already existed, no changes needed

**Workspace Settings** (WolfPackAI Project Only):
- File: `C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI\.vscode\settings.json`
- Uses: Ollama direct (http://localhost:1143/v1)
- Created new for this project

**Why Both?** Workspace settings override global settings. This project uses Ollama for speed and simplicity.

---

## Available Models

**Local (FREE, Fast):**
- `deepseek-coder-v2:16b` - Primary coding model
- `deepseek-coder-v2:latest` - Same as above

**Cloud (Requires API Keys):**
- `claude-sonnet-4`, `claude-opus-4`, `claude-haiku-4`
- `gpt-4-turbo`, `gpt-4`, `gpt-3.5-turbo`
- `gemini-pro`

Note: Cloud models available through LiteLLM if you add API keys

---

## Quick Commands

### Verify Services Running
```bash
# Check Ollama
curl http://localhost:1143/v1/models

# Check LiteLLM
curl http://localhost:4000/v1/models -H "Authorization: Bearer sk-dev-1234"
```

### Restart Services (if needed)
```bash
cd C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI
dotnet run --project WolfPackAI.AppHost
```

---

## Service Endpoints

| Service | URL | Auth |
|---------|-----|------|
| Ollama | http://localhost:1143 | None |
| LiteLLM | http://localhost:4000 | sk-dev-1234 |
| OpenWebUI | http://localhost:8080 | Web UI |

---

## Switch to LiteLLM (Optional)

If you want advanced features (routing, fallbacks, analytics):

1. Open `.vscode\settings.json`
2. Find "Alternative Configuration" section
3. **Uncomment** LiteLLM lines:
   ```json
   "cursor.general.apiBaseUrl": "http://localhost:4000/v1",
   "openai.api.baseUrl": "http://localhost:4000/v1",
   "openai.api.key": "sk-dev-1234",
   ```
4. **Comment out** Ollama lines
5. Save and reload Cursor

---

## Troubleshooting

**Problem:** Model not found
- **Solution:** Verify services running with curl commands above

**Problem:** Slow responses
- **Solution:** You're using Ollama direct, should be fast. Check system resources.

**Problem:** Authentication error
- **Solution:** If using LiteLLM, verify key is `sk-dev-1234` (not `sk-1234`)

---

## Full Documentation

For complete details, see:
`C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI\docs\CURSOR_SETUP_COMPLETED.md`

---

**Ready to Code!** Just restart Cursor and you're all set.
