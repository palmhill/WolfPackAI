#!/usr/bin/env pwsh
<#
.SYNOPSIS
    WolfPackAI Bootstrap Script - One-Click Launch
.DESCRIPTION
    Validates prerequisites, installs dependencies, runs MOD SQUAD pre-flight checks,
    and launches the entire WolfPackAI platform via .NET Aspire.
.EXAMPLE
    .\bootstrap.ps1
#>

param(
    [switch]$SkipChecks,
    [switch]$SkipInstall,
    [int]$Timeout = 120
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "🐺 WolfPackAI Bootstrap" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Prerequisite Checks
if (-not $SkipChecks) {
    Write-Host "📋 Checking Prerequisites..." -ForegroundColor Yellow
    
    # Check ports availability
    Write-Host "  🔍 Checking port availability..." -ForegroundColor Cyan
    $requiredPorts = @(5432, 1143, 4000, 5678, 8000, 8080)
    $portsInUse = @()
    
    foreach ($port in $requiredPorts) {
        $connection = Test-NetConnection -ComputerName localhost -Port $port -WarningAction SilentlyContinue -InformationLevel Quiet
        if ($connection) {
            $portsInUse += $port
        }
    }
    
    if ($portsInUse.Count -gt 0) {
        Write-Host "  ⚠️  Warning: The following ports are already in use:" -ForegroundColor Yellow
        foreach ($port in $portsInUse) {
            Write-Host "     - Port $port" -ForegroundColor Yellow
        }
        Write-Host "     You may need to stop conflicting services or WolfPackAI may fail to start." -ForegroundColor Yellow
        Write-Host "     Run 'netstat -ano | findstr :<PORT>' to find what's using a port." -ForegroundColor Gray
        Write-Host ""
        
        $response = Read-Host "Continue anyway? (y/N)"
        if ($response -ne 'y' -and $response -ne 'Y') {
            Write-Host "❌ Aborted by user" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "  ✅ All required ports available" -ForegroundColor Green
    }
    
    # Check .NET SDK
    try {
        $dotnetVersion = dotnet --version
        Write-Host "  ✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
    } catch {
        Write-Host "  ❌ .NET SDK not found!" -ForegroundColor Red
        Write-Host "     Install from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
        exit 1
    }
    
    # Check Aspire workload
    $aspireInstalled = dotnet workload list | Select-String "aspire"
    if (-not $aspireInstalled) {
        Write-Host "  ⚠️  .NET Aspire workload not installed" -ForegroundColor Yellow
        Write-Host "     Installing Aspire workload..." -ForegroundColor Yellow
        dotnet workload install aspire
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  ❌ Failed to install Aspire workload" -ForegroundColor Red
            exit 1
        }
        Write-Host "  ✅ Aspire workload installed" -ForegroundColor Green
    } else {
        Write-Host "  ✅ .NET Aspire workload installed" -ForegroundColor Green
    }
    
    # Check Docker
    try {
        $dockerVersion = docker --version
        $dockerRunning = docker ps 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  ❌ Docker is installed but not running!" -ForegroundColor Red
            Write-Host "     Please start Docker Desktop and try again." -ForegroundColor Yellow
            exit 1
        }
        Write-Host "  ✅ Docker: $dockerVersion (running)" -ForegroundColor Green
    } catch {
        Write-Host "  ❌ Docker not found!" -ForegroundColor Red
        Write-Host "     Install from: https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
        exit 1
    }
    
    # Check Python
    try {
        $pythonVersion = python --version
        Write-Host "  ✅ Python: $pythonVersion" -ForegroundColor Green
    } catch {
        Write-Host "  ⚠️  Python not found (optional for MOD SQUAD)" -ForegroundColor Yellow
        Write-Host "     Install from: https://www.python.org/downloads/" -ForegroundColor Yellow
    }
    
    # Check Node.js
    try {
        $nodeVersion = node --version
        Write-Host "  ✅ Node.js: $nodeVersion" -ForegroundColor Green
    } catch {
        Write-Host "  ⚠️  Node.js not found (optional for MOD SQUAD)" -ForegroundColor Yellow
        Write-Host "     Install from: https://nodejs.org/" -ForegroundColor Yellow
    }
    
    Write-Host ""
}

# Step 2: Install Dependencies
if (-not $SkipInstall) {
    Write-Host "📦 Installing Dependencies..." -ForegroundColor Yellow
    
    # Restore NuGet packages
    Write-Host "  🔄 Restoring NuGet packages..." -ForegroundColor Cyan
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ❌ Failed to restore NuGet packages" -ForegroundColor Red
        exit 1
    }
    Write-Host "  ✅ NuGet packages restored" -ForegroundColor Green
    
    # Install Python dependencies (if Python available)
    if (Get-Command python -ErrorAction SilentlyContinue) {
        if (Test-Path "requirements.txt") {
            Write-Host "  🔄 Installing Python dependencies..." -ForegroundColor Cyan
            python -m pip install --quiet --upgrade pip
            python -m pip install --quiet -r requirements.txt
            if ($LASTEXITCODE -eq 0) {
                Write-Host "  ✅ Python dependencies installed" -ForegroundColor Green
            } else {
                Write-Host "  ⚠️  Python dependencies failed (continuing anyway)" -ForegroundColor Yellow
            }
        }
    }
    
    # Install npm dependencies (if Node.js available)
    if (Get-Command npm -ErrorAction SilentlyContinue) {
        if (Test-Path "package.json") {
            Write-Host "  🔄 Installing npm dependencies..." -ForegroundColor Cyan
            npm install --silent
            if ($LASTEXITCODE -eq 0) {
                Write-Host "  ✅ npm dependencies installed" -ForegroundColor Green
            } else {
                Write-Host "  ⚠️  npm dependencies failed (continuing anyway)" -ForegroundColor Yellow
            }
        }
    }
    
    Write-Host ""
}

# Step 3: MOD SQUAD Pre-flight Checks
if (-not $SkipChecks) {
    Write-Host "🔍 Running MOD SQUAD Pre-flight Checks..." -ForegroundColor Yellow
    
    if (Test-Path "scripts/wolfpack_repo_audit.py") {
        Write-Host "  🔄 Repository audit..." -ForegroundColor Cyan
        python scripts/wolfpack_repo_audit.py --output reports/bootstrap-audit.json
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✅ Repository audit passed" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️  Repository audit found issues (see reports/bootstrap-audit.json)" -ForegroundColor Yellow
            Write-Host "     Continuing with launch..." -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
}

# Step 4: Launch Aspire AppHost
Write-Host "🚀 Launching WolfPackAI via Aspire..." -ForegroundColor Yellow
Write-Host ""

$aspireJob = Start-Job -ScriptBlock {
    param($ProjectPath)
    Set-Location $ProjectPath
    dotnet run --project WolfPackAI.AppHost
} -ArgumentList (Get-Location).Path

Write-Host "  ⏳ Aspire AppHost starting (Job ID: $($aspireJob.Id))..." -ForegroundColor Cyan
Write-Host "     Waiting for services to become healthy (timeout: ${Timeout}s)..." -ForegroundColor Cyan
Write-Host ""

# Step 5: Wait for Services
Write-Host "🔍 Checking for first-run Ollama model download..." -ForegroundColor Yellow

# Detect first run by checking if models are cached
$isFirstRun = $false
try {
    $ollamaCheck = docker ps --format "{{.Names}}" 2>$null | Select-String "ollama"
    if ($ollamaCheck) {
        $modelCheck = docker exec $ollamaCheck.Line ollama list 2>$null
        if (-not $modelCheck -or $modelCheck.Length -lt 50) {
            $isFirstRun = $true
            $Timeout = 600  # Extend to 10 minutes for first run
            Write-Host "  ⏳ First run detected! Ollama will download AI models (5-10 minutes)..." -ForegroundColor Cyan
            Write-Host "     Extended timeout to ${Timeout}s. Please be patient..." -ForegroundColor Cyan
        }
    }
} catch {
    # Docker not ready yet, proceed with default timeout
}

Write-Host ""

$startTime = Get-Date
$healthCheckInterval = 5
$attempt = 0

if (Test-Path "scripts/wolfpack_health_check.py") {
    while (((Get-Date) - $startTime).TotalSeconds -lt $Timeout) {
        $attempt++
        $elapsed = [int]((Get-Date) - $startTime).TotalSeconds
        
        if ($isFirstRun) {
            Write-Host "  🔄 Attempt $attempt (${elapsed}s elapsed) - Downloading models..." -ForegroundColor Cyan
        } else {
            Write-Host "  🔄 Attempt $attempt (${elapsed}s elapsed)..." -ForegroundColor Cyan
        }
        
        python scripts/wolfpack_health_check.py --config mod_squad.config.json 2>$null
        
        if ($LASTEXITCODE -eq 0) {
            $totalTime = [int]((Get-Date) - $startTime).TotalSeconds
            Write-Host ""
            Write-Host "  ✅ All services healthy after ${totalTime}s!" -ForegroundColor Green
            break
        }
        
        $remaining = $Timeout - $elapsed
        if ($remaining -gt 0) {
            Write-Host "     Retrying in ${healthCheckInterval}s... (${remaining}s remaining)" -ForegroundColor Gray
            Start-Sleep -Seconds $healthCheckInterval
        }
    }
    
    if (((Get-Date) - $startTime).TotalSeconds -ge $Timeout) {
        Write-Host ""
        Write-Host "  ⚠️  Timeout reached. Some services may still be starting." -ForegroundColor Yellow
        Write-Host "     Check Aspire Dashboard for service status." -ForegroundColor Yellow
        if ($isFirstRun) {
            Write-Host "     Note: Large AI models may still be downloading." -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "  ⚠️  Health check script not found, waiting 30s..." -ForegroundColor Yellow
    Start-Sleep -Seconds 30
}

Write-Host ""

# Step 6: Open Browser
Write-Host "🌐 Opening Aspire Dashboard..." -ForegroundColor Yellow

$dashboardUrl = "http://localhost:15021"
Start-Process $dashboardUrl

Write-Host ""
Write-Host "✅ WolfPackAI is running!" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Service URLs:" -ForegroundColor Cyan
Write-Host "  • Aspire Dashboard:  http://localhost:15021" -ForegroundColor White
Write-Host "  • OpenWebUI:         http://localhost:8080" -ForegroundColor White
Write-Host "  • LiteLLM:           http://localhost:4000" -ForegroundColor White
Write-Host "  • n8n:               http://localhost:5678" -ForegroundColor White
Write-Host "  • Dashboard:         http://localhost:8000" -ForegroundColor White
Write-Host ""
Write-Host "🛑 To stop: Press Ctrl+C in this window" -ForegroundColor Yellow
Write-Host ""

# Keep script running and show Aspire output
Write-Host "📋 Aspire AppHost Output:" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray

try {
    Receive-Job -Job $aspireJob -Wait
} catch {
    Write-Host ""
    Write-Host "⚠️  Aspire AppHost stopped" -ForegroundColor Yellow
}

# Cleanup
Stop-Job -Job $aspireJob -ErrorAction SilentlyContinue
Remove-Job -Job $aspireJob -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "👋 WolfPackAI stopped" -ForegroundColor Cyan

