# WolfPackAI MOD SQUAD Assessment

**Date:** October 30, 2025  
**Analyst:** Dr. Cursor Claude  
**Project:** WolfPackAI Standalone Setup

---

## Executive Summary

WolfPackAI is a .NET Aspire-based AI orchestration platform managing 5 containerized microservices. This assessment maps the current architecture, identifies health endpoints, documents authentication patterns, and defines MOD SQUAD implementation requirements for achieving "click-and-run" deployment.

---

## Architecture Overview

### Solution Structure
- **WolfPackAI.AppHost** - Aspire orchestration entry point (`Program.cs`)
- **WolfPackAI.AppBuilder** - Service extension methods and configuration classes
- **WolfPackAI.ServiceDefaults** - Shared service configuration
- **WolfPackAI.Dashboard** - ASP.NET Core proxy/portal (port 80/443)

### Managed Services (5 Containers)

| Service | Image | Internal Port | External Port | Purpose |
|---------|-------|---------------|---------------|---------|
| **PostgreSQL** | postgres | 5432 | 5432 | Primary database (3 databases: openwebuidb, litellmdb, n8ndb) |
| **Ollama** | ollama/ollama | 11434 | 1143 | Local LLM runtime (deepseek-coder-v2:16b) |
| **LiteLLM** | ghcr.io/berriai/litellm-database | 4000 | 4000 | LLM API gateway with routing |
| **OpenWebUI** | ghcr.io/open-webui/open-webui | 8080 | 8080 | Chat interface |
| **n8n** | n8n | 5678 | 5678 | Workflow automation |

### Service Dependencies
```
PostgreSQL (base)
  ├─> Ollama (waits for nothing)
  ├─> LiteLLM (waits for PostgreSQL, Ollama)
  ├─> OpenWebUI (waits for PostgreSQL, LiteLLM)
  └─> n8n (waits for PostgreSQL)

Dashboard (waits for OpenWebUI, LiteLLM, n8n)
```

---

## Health Check Endpoints

| Service | Health URL | Expected Response | Method | Auth Required |
|---------|------------|-------------------|--------|---------------|
| **PostgreSQL** | N/A (use `pg_isready`) | Connection success | CLI | No |
| **Ollama** | `http://localhost:1143/api/tags` | 200 OK with JSON | GET | No |
| **LiteLLM** | `http://localhost:4000/health` | 200 OK (disabled in code due to auth) | GET | Yes (Bearer token) |
| **OpenWebUI** | `http://localhost:8080/health` | 200 OK | GET | No |
| **n8n** | `http://localhost:5678/healthz` | 200 OK (assumed) | GET | No |
| **Dashboard** | `http://localhost:8000/` or `:443` | 200 OK | GET | No |

**Note:** LiteLLM health check is currently disabled in `Program.cs` (line 61 comment).

---

## Authentication Patterns

### 1. PostgreSQL
- **Type:** Username/Password
- **Credentials:** `postgres` / `postgres` (from `appsettings.json`)
- **Storage:** `appsettings.json` → Postgres config section
- **Used By:** All services connecting to databases

### 2. LiteLLM
- **Type:** API Key (Master Key)
- **Credentials:** `sk-1234` (from `appsettings.json`)
- **Storage:** `appsettings.json` → LiteLLM.GeneralSettings.MasterKey
- **Used By:** OpenWebUI calls to LiteLLM proxy
- **UI Auth:** Username/Password (`test`/`test` hardcoded in extension)

### 3. OpenWebUI
- **Type:** Internal auth + Azure AD OAuth (configured but not enforced)
- **Credentials:** User registration required on first launch
- **Storage:** PostgreSQL `openwebuidb`
- **Public URL:** `http://localhost/chat` (from config)

### 4. n8n
- **Type:** Webhook tokens (assumed)
- **Credentials:** Not explicitly configured in current setup
- **Storage:** PostgreSQL `n8ndb`

### 5. Ollama
- **Type:** None (local API)
- **Access:** Direct HTTP calls, no auth

---

## Existing Automation

### GitHub Workflows

#### `.github/workflows/ci.yml`
- **Triggers:** Push to any branch, pull requests
- **Actions:**
  - Restores NuGet dependencies
  - Builds solution in Release mode
  - Validates AppHost project structure
  - Runs tests (if any exist)
  - Creates build summary
- **Gaps:**
  - No health check validation
  - No browser/UI testing
  - No config audit
  - No service orchestration test

#### `.github/workflows/release.yml`
- **Purpose:** Release automation (not analyzed in detail)

### Build Scripts
- **None present** - relies on `dotnet build` / `dotnet run`

---

## "Click-and-Run" Gaps Analysis

### Current Manual Steps
1. **Prerequisites:** User must manually install .NET SDK 9.0, Docker Desktop, Aspire workload
2. **Dependencies:** User must run `dotnet restore` manually
3. **Configuration:** User must edit `appsettings.json` with credentials
4. **Docker:** User must start Docker Desktop before running
5. **Launch:** User must run `dotnet run --project WolfPackAI.AppHost`
6. **Waiting:** User must manually check when services are ready
7. **Access:** User must manually open browser to Dashboard URL

### Missing Automation
- ✗ Prerequisite validation script
- ✗ Dependency installation automation
- ✗ Pre-flight health checks
- ✗ Service readiness polling
- ✗ Automatic browser launch
- ✗ Error diagnostics and troubleshooting
- ✗ Pre-commit validation hooks
- ✗ CI-gated health checks

---

## MOD SQUAD Implementation Requirements

### Phase 2: Scripts & Config

#### 1. `mod_squad.config.json`
```json
{
  "services": {
    "postgres": {
      "type": "database",
      "health_command": "pg_isready -h localhost -p 5432 -U postgres"
    },
    "ollama": {
      "url": "http://localhost:1143",
      "health": "/api/tags",
      "timeout_ms": 10000
    },
    "litellm": {
      "url": "http://localhost:4000",
      "health": "/health",
      "auth": "bearer",
      "timeout_ms": 5000
    },
    "openwebui": {
      "url": "http://localhost:8080",
      "health": "/health",
      "timeout_ms": 5000
    },
    "n8n": {
      "url": "http://localhost:5678",
      "health": "/healthz",
      "timeout_ms": 5000
    },
    "dashboard": {
      "url": "http://localhost:80",
      "health": "/",
      "timeout_ms": 3000
    }
  },
  "thresholds": {
    "p95_ms": 5000,
    "startup_timeout_s": 120
  }
}
```

#### 2. `scripts/wolfpack_health_check.py`
- Poll all 6 services (PostgreSQL + 5 containers)
- Handle LiteLLM auth if enabled
- Retry logic with exponential backoff
- Output: JSON report with timings

#### 3. `scripts/wolfpack_browser_mod.py`
- Playwright tests for:
  - Dashboard (`http://localhost:80`)
  - OpenWebUI (`http://localhost:8080`)
- Check for console errors, failed requests
- Screenshot on failure
- Output: JSON report

#### 4. `scripts/wolfpack_repo_audit.py`
- Scan `appsettings.json` for:
  - Missing required config sections
  - Hardcoded secrets (detect patterns)
  - Config drift between projects
- Validate Docker configs
- Output: JSON report with severity levels

#### 5. `package.json`
```json
{
  "scripts": {
    "mod:health": "python scripts/wolfpack_health_check.py --output reports/health.json",
    "mod:browser": "python scripts/wolfpack_browser_mod.py --output reports/browser.json",
    "mod:audit": "python scripts/wolfpack_repo_audit.py --output reports/audit.json",
    "mod:all": "npm run mod:health && npm run mod:browser && npm run mod:audit"
  },
  "devDependencies": {
    "playwright": "^1.40.0"
  }
}
```

### Phase 3: Bootstrap Script

#### `bootstrap.ps1` Requirements
1. **Prerequisite Checks:**
   - .NET SDK 9.0+ installed
   - Docker Desktop installed and running
   - Python 3.9+ installed
   - Node.js 22+ installed
   - Aspire workload installed (`dotnet workload list`)

2. **Dependency Installation:**
   - `dotnet restore`
   - `pip install -r requirements.txt` (requests, playwright)
   - `npm install` (playwright browsers)

3. **Pre-flight MOD Checks:**
   - Run `npm run mod:audit` (fail fast on config issues)

4. **Aspire Launch:**
   - `Start-Process` to run `dotnet run --project WolfPackAI.AppHost` in background
   - Capture PID for cleanup

5. **Service Readiness:**
   - Run `python scripts/wolfpack_health_check.py --wait --timeout 120`
   - Poll every 5s until all services healthy or timeout

6. **Browser Launch:**
   - `Start-Process "http://localhost:15021"` (Aspire Dashboard)

7. **Success Message:**
   - Print service URLs
   - Show next steps

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| **PostgreSQL not ready before dependent services** | High - cascading failures | Add retry logic with 30s max wait per service |
| **Ollama model download** | High - first launch takes 5-10 min | Bootstrap script warns user, extends timeout to 600s |
| **LiteLLM auth blocks health check** | Medium - can't validate service | Use `/health` endpoint without auth or mock credentials |
| **Docker Desktop not running** | High - immediate failure | Bootstrap validates `docker ps` before proceeding |
| **Port conflicts** | Medium - services fail to bind | Bootstrap checks ports 80, 443, 4000, 5432, 5678, 8080, 1143 |
| **Missing Aspire workload** | High - build fails | Bootstrap validates `dotnet workload list | grep aspire` |

---

## Success Criteria

### Phase 2 Complete
- ✅ `npm run mod:all` executes without errors
- ✅ Health check validates all 5 services
- ✅ Browser tests pass for Dashboard + OpenWebUI
- ✅ Repo audit detects config issues
- ✅ CI workflow blocks PRs with MOD failures

### Phase 3 Complete
- ✅ `.\bootstrap.ps1` launches entire platform in <3 minutes (excluding first Ollama download)
- ✅ All services healthy and accessible
- ✅ Dashboard opens automatically in browser
- ✅ Clear error messages on failure
- ✅ Troubleshooting guide covers common issues

---

## Next Steps

1. **Implement Phase 2:** Create config, scripts, npm commands, CI workflow
2. **Test Phase 2:** Run `npm run mod:all` and validate outputs
3. **Implement Phase 3:** Build bootstrap script and quickstart guide
4. **Test Phase 3:** Fresh machine test with zero manual steps
5. **Implement Phase 4:** Extract reusable MOD SQUAD template

---

**Assessment Complete** ✅  
Ready to proceed with implementation.

