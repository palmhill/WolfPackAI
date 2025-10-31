@echo off
REM WolfPackAI Complete Auto-Start
REM Starts: Docker, WolfPackAI, Gatekeeper, PowerShell, Terminal, Cursor

echo.
echo ============================================
echo  WolfPackAI Complete Auto-Start
echo ============================================
echo.

REM Step 1: Start Docker Desktop if not running
echo [1/6] Starting Docker Desktop...
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo   Docker not running, starting...
    start "" "C:\Program Files\Docker\Docker\Docker Desktop.exe"
    echo   Waiting 45 seconds for Docker...
    timeout /t 45 /nobreak
) else (
    echo   Docker already running!
)

REM Step 2: Start WolfPackAI Services
echo.
echo [2/6] Starting WolfPackAI Services...
start "WolfPackAI Services" cmd /k "cd /d C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI && echo [WolfPackAI Services] Starting... && dotnet run --project WolfPackAI.AppHost"
echo   Waiting 20 seconds for services...
timeout /t 20 /nobreak

REM Step 3: Start Gatekeeper
echo.
echo [3/6] Starting WolfPackAI Gatekeeper...
start "WolfPackAI Gatekeeper" cmd /k "cd /d C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI && echo [Gatekeeper] Starting on port 7000... && dotnet run --project WolfPackAI.Gatekeeper"

REM Step 4: Open PowerShell in WolfPackAI directory
echo.
echo [4/6] Opening PowerShell...
start "PowerShell - WolfPackAI" powershell -NoExit -Command "cd C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI; Write-Host '🐺 WolfPackAI PowerShell Ready!' -ForegroundColor Cyan"

REM Step 5: Open Windows Terminal in WolfPackAI directory
echo.
echo [5/6] Opening Windows Terminal...
wt.exe -d C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI 2>nul
if %errorlevel% neq 0 (
    echo   Windows Terminal not found, skipping...
)

REM Step 6: Wait a bit then open Cursor
echo.
echo [6/6] Opening Cursor IDE...
timeout /t 5 /nobreak
start "" "C:\Users\SSaint-Cyr\AppData\Local\Programs\cursor\Cursor.exe" "C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI"

echo.
echo ============================================
echo  ✅ All Services Started!
echo ============================================
echo.
echo Running:
echo   ✅ Docker Desktop
echo   ✅ WolfPackAI Services (http://localhost:5000)
echo   ✅ Gatekeeper (http://localhost:7000)
echo   ✅ PowerShell (in WolfPackAI directory)
echo   ✅ Windows Terminal (in WolfPackAI directory)
echo   ✅ Cursor IDE (WolfPackAI project)
echo.
echo Cursor Integration:
echo   - Look for [🐺 N LLMs] in status bar
echo   - All 15+ models ready in Auto mode!
echo.
echo Aspire Dashboard:
echo   - Check "WolfPackAI Services" window for URL
echo.
timeout /t 10
exit
