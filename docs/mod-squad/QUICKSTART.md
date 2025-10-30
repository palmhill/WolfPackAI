# WolfPackAI MOD SQUAD Quickstart

## 🚀 One-Command Launch

```powershell
.\bootstrap.ps1
```

That's it! The bootstrap script will:
1. ✅ Validate prerequisites (.NET SDK, Docker, Python, Node.js)
2. 📦 Install all dependencies (NuGet, pip, npm)
3. 🔍 Run MOD SQUAD pre-flight checks
4. 🚀 Launch all services via Aspire
5. ⏳ Wait for services to become healthy
6. 🌐 Open Dashboard in your browser

---

## Prerequisites

### Required
- ✅ **.NET SDK 9.0+** - [Download](https://dotnet.microsoft.com/download)
- ✅ **Docker Desktop** (running) - [Download](https://www.docker.com/products/docker-desktop)

### Optional (for MOD SQUAD)
- ⚠️ **Python 3.9+** - [Download](https://www.python.org/downloads/)
- ⚠️ **Node.js 18+** - [Download](https://nodejs.org/)

---

## Manual Setup (if needed)

### 1. Install .NET Aspire Workload
```powershell
dotnet workload install aspire
```

### 2. Restore Dependencies
```powershell
# .NET packages
dotnet restore

# Python packages (if Python installed)
pip install -r requirements.txt

# npm packages (if Node.js installed)
npm install
```

### 3. Launch Manually
```powershell
dotnet run --project WolfPackAI.AppHost
```

---

## MOD SQUAD Commands

### Health Check (all services)
```powershell
npm run mod:health
```

### Browser Tests (Dashboard + OpenWebUI)
```powershell
npm run mod:browser
```

### Repository Audit (config scan)
```powershell
npm run mod:audit
```

### Run All Checks
```powershell
npm run mod:all
```

### Quick Health Check (critical services only)
```powershell
npm run mod:quick
```

### Wait for Services (with timeout)
```powershell
npm run mod:wait
```

---

## Service URLs

Once running, access services at:

| Service | URL | Purpose |
|---------|-----|---------|
| **Aspire Dashboard** | http://localhost:15021 | Monitor all services |
| **OpenWebUI** | http://localhost:8080 | AI chat interface |
| **LiteLLM** | http://localhost:4000 | LLM API gateway |
| **n8n** | http://localhost:5678 | Workflow automation |
| **Dashboard** | http://localhost:80 | Web portal |
| **PostgreSQL** | localhost:5432 | Database |

---

## Troubleshooting

### Issue: "Docker is not running"
**Solution:**
1. Open Docker Desktop
2. Wait for Docker to fully start (whale icon in system tray)
3. Run bootstrap script again

### Issue: "Aspire workload not found"
**Solution:**
```powershell
dotnet workload install aspire
```

### Issue: "Port already in use"
**Solution:**
```powershell
# Check what's using the port
netstat -ano | findstr :8080

# Stop conflicting containers
docker ps
docker stop <container-id>
```

### Issue: "Services timeout / not healthy"
**Possible causes:**
1. **First run:** Ollama downloads models (5-10 minutes)
2. **PostgreSQL slow:** Database initializing
3. **Port conflicts:** Check ports 80, 4000, 5432, 5678, 8080, 1143

**Solution:**
```powershell
# Check service status in Aspire Dashboard
# http://localhost:15021

# Or run health check manually
python scripts/wolfpack_health_check.py --wait --timeout 300
```

### Issue: "Python not found"
**Solution:**
1. Install Python from https://www.python.org/downloads/
2. **Important:** Check "Add Python to PATH" during installation
3. Restart PowerShell and try again

### Issue: "Module 'requests' not found"
**Solution:**
```powershell
pip install -r requirements.txt
```

### Issue: "Playwright browsers not installed"
**Solution:**
```powershell
python -m playwright install
```

---

## Advanced Usage

### Skip Prerequisite Checks
```powershell
.\bootstrap.ps1 -SkipChecks
```

### Skip Dependency Installation
```powershell
.\bootstrap.ps1 -SkipInstall
```

### Custom Timeout (default: 120s)
```powershell
.\bootstrap.ps1 -Timeout 300
```

### Combine Options
```powershell
.\bootstrap.ps1 -SkipChecks -Timeout 300
```

---

## Configuration

### Edit Service Settings
Edit `WolfPackAI.AppHost/appsettings.json`:

```json
{
  "LiteLLM": {
    "GeneralSettings": {
      "MasterKey": "your-key-here"
    },
    "ModelList": [...]
  },
  "Postgres": {
    "Username": "postgres",
    "Password": "your-password"
  },
  ...
}
```

### Edit MOD SQUAD Config
Edit `mod_squad.config.json`:

```json
{
  "thresholds": {
    "p95_response_ms": 5000,
    "startup_timeout_s": 120
  },
  ...
}
```

---

## Stopping WolfPackAI

1. Press `Ctrl+C` in the bootstrap script window
2. Or close the PowerShell window
3. All containers will stop automatically

To manually stop containers:
```powershell
docker ps
docker stop <container-id>
```

---

## Development Workflow

### 1. Start Platform
```powershell
.\bootstrap.ps1
```

### 2. Make Changes
Edit code in your IDE (Visual Studio, VS Code, etc.)

### 3. Rebuild (if needed)
```powershell
dotnet build
```

### 4. Restart Aspire
Press `Ctrl+C` in bootstrap window, then run `.\bootstrap.ps1` again

---

## CI/CD Integration

### GitHub Actions
MOD SQUAD checks run automatically on push/PR via `.github/workflows/wolfpack-mod-squad.yml`

### Pre-commit Hook
Quick validation runs before every commit via `.git/hooks/pre-commit`

To skip (not recommended):
```powershell
git commit --no-verify -m "message"
```

---

## Next Steps

1. ✅ **Explore Aspire Dashboard** - http://localhost:15021
2. ✅ **Try OpenWebUI** - http://localhost:8080 (create account on first visit)
3. ✅ **Configure n8n** - http://localhost:5678 (setup on first visit)
4. ✅ **Run MOD SQUAD checks** - `npm run mod:all`
5. ✅ **Review architecture** - See `docs/mod-squad/ASSESSMENT.md`

---

## Getting Help

- **Architecture details:** `docs/mod-squad/ASSESSMENT.md`
- **Progress tracking:** `docs/mod-squad/PROGRESS.md`
- **MOD SQUAD config:** `mod_squad.config.json`
- **Main README:** `README.md`

---

**🐺 Happy coding with WolfPackAI!**

