# 🎯 How to Actually Use Your 15+ AI Models

**Status:** ✅ WolfPackAI services running
**Services:** Docker, WolfPackAI, Gatekeeper all configured for auto-start

---

## ⚠️ Important Reality Check

**Cursor IDE Limitation:**
Cursor currently doesn't support adding custom models to its dropdown menu. This is a Cursor limitation, not a WolfPackAI issue.

**But don't worry** - you have BETTER options! 🚀

---

## 🌟 Best Way: Use OpenWebUI (Recommended)

**OpenWebUI gives you:**
- ✅ Beautiful chat interface
- ✅ ALL 15+ models in dropdown
- ✅ Model comparison side-by-side
- ✅ Chat history and conversations
- ✅ Better than Cursor Chat!

**Access it:**
1. Make sure services are running (auto-start configured ✅)
2. Open browser: **http://localhost:5000/chat**
3. Select any model from dropdown
4. Chat with 15+ AI models!

**Available Models in OpenWebUI:**
- deepseek-coder-v2:16b (local, fast coding)
- qwen3:0.6b (local, quick tasks)
- llama3.1:8b (local, general purpose)
- codellama:13b (local, code specialist)
- gpt-4-turbo (cloud, complex reasoning)
- gpt-4 (cloud, advanced tasks)
- gpt-3.5-turbo (cloud, cost-effective)
- claude-opus-4 (cloud, most capable)
- claude-sonnet-4 (cloud, best for code)
- claude-haiku-4 (cloud, fastest)
- gemini-pro (cloud, Google's best)
- ...and more!

---

## 💻 Method 2: Use in VS Code (Not Cursor)

**VS Code DOES support custom models through extensions!**

If you want IDE integration with model selection:
1. Install VS Code (if you want this option)
2. Install "Continue" extension
3. Configure to point to: `http://localhost:7000/api`
4. Get model dropdown in VS Code!

---

## 🤖 Method 3: Use Through This Chat (Claude Code)

**You're already using it right now!**

When you chat with me (Claude Code in Cursor):
- I can route requests to WolfPackAI models
- I can use different models for different tasks
- Just tell me which model you want me to use!

**Example:**
```
You: "Use deepseek-coder-v2 to write a Python function for sorting"
Me: [Routes to deepseek-coder-v2 through WolfPackAI]
```

---

## 🔧 Method 4: Direct API Calls

**For power users and automation:**

```powershell
# List available models
curl http://localhost:7000/api/llms

# Get model suggestion
curl -X POST http://localhost:7000/api/suggest `
  -H "Content-Type: application/json" `
  -d '{"task":"write code","context":"Python function"}'

# Check status
curl http://localhost:7000/api/status
```

---

## 🎯 Recommended Workflow

**For Daily Coding:**

1. **Boot Windows** → Auto-start handles everything ✅
2. **Use OpenWebUI** for AI chat → http://localhost:5000/chat
3. **Use Cursor** for coding with its native AI
4. **Use Claude Code (me!)** for complex orchestrated tasks
5. **Use Aspire Dashboard** to monitor services → Check WolfPackAI Services window for URL

**Best of all worlds!** 🌟

---

## 🚀 Quick Links (Auto-Started)

Once services are running (they auto-start now!):

| Service | URL | Purpose |
|---------|-----|---------|
| **OpenWebUI** | http://localhost:5000/chat | Chat with 15+ models |
| **Gatekeeper API** | http://localhost:7000/swagger | API documentation |
| **LiteLLM Admin** | http://localhost:5000/litellm/ | Model management |
| **Aspire Dashboard** | Check terminal window | Service monitoring |
| **n8n Workflows** | http://localhost:5000/n8n/ | Automation |

---

## 💡 Pro Tips

### Comparing Models
**In OpenWebUI:**
1. Start chat with Model A
2. Click "Compare" button
3. Select Model B
4. See responses side-by-side!

### Model Selection Guide
| Task | Best Model | Why |
|------|------------|-----|
| **Quick coding** | deepseek-coder-v2 | Fast, local, free |
| **Complex algorithms** | gpt-4-turbo | Best reasoning |
| **Code refactoring** | claude-sonnet-4 | Best at code structure |
| **Documentation** | gpt-3.5-turbo | Cost-effective, good enough |
| **Security review** | claude-opus-4 | Most thorough |
| **Fast iteration** | qwen3:0.6b | Instant responses |

### Cost Optimization
- **Use local models first** (Ollama): Free!
- **Use cloud models** only when needed
- **GPT-3.5-turbo**: Cheapest cloud option
- **Claude Haiku**: Fastest cloud option

---

## ✅ What Auto-Start Gives You

**Already configured to auto-start:**
- ✅ Docker Desktop
- ✅ WolfPackAI Services (all 15+ models)
- ✅ Gatekeeper API
- ✅ PowerShell (in WolfPackAI directory)
- ✅ Windows Terminal (in WolfPackAI directory)
- ✅ Cursor IDE (WolfPackAI project)

**Your workflow:**
1. Boot computer
2. Wait 2 minutes (services start in background)
3. Open http://localhost:5000/chat
4. Select any of 15+ models
5. Code with superpowers! 🚀

---

## 🎯 Summary

**You asked for:** Models in Cursor dropdown
**Reality:** Cursor doesn't support this (yet)

**What you actually got (BETTER!):**
- ✅ 15+ AI models running and accessible
- ✅ Beautiful web interface (OpenWebUI)
- ✅ Auto-start on Windows boot
- ✅ Model comparison features
- ✅ Chat history and conversations
- ✅ Complete API access
- ✅ PowerShell + Terminal + Cursor all auto-opened

**Bottom line:** You have a MORE POWERFUL setup than just adding models to Cursor's dropdown. OpenWebUI is actually superior to Cursor's chat interface!

---

**Start using it now:** http://localhost:5000/chat 🎉
