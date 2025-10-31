@echo off
echo.
echo ========================================
echo  WolfPackAI Full Stack Auto-Start
echo ========================================
echo.

REM Check if Docker Desktop is running
echo [1/3] Checking Docker Desktop...
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo   Docker Desktop is not running
    echo   Starting Docker Desktop...
    start "" "C:\Program Files\Docker\Docker\Docker Desktop.exe"
    echo   Waiting 45 seconds for Docker to start...
    timeout /t 45 /nobreak
) else (
    echo   Docker Desktop is already running
)

echo.
echo [2/3] Starting WolfPackAI Services...
start "WolfPackAI Services" cmd /k "cd /d %~dp0 && echo Starting WolfPackAI Services... && dotnet run --project WolfPackAI.AppHost"
echo   Waiting 20 seconds for services to initialize...
timeout /t 20 /nobreak

echo.
echo [3/3] Starting Gatekeeper...
start "WolfPackAI Gatekeeper" cmd /k "cd /d %~dp0 && echo Starting Gatekeeper... && dotnet run --project WolfPackAI.Gatekeeper"

echo.
echo ========================================
echo  All Services Started!
echo ========================================
echo.
echo Services:
echo   - Docker Desktop: Running
echo   - WolfPackAI: http://localhost:5000
echo   - Gatekeeper: http://localhost:7000
echo   - Aspire Dashboard: Check WolfPackAI window
echo.
echo Cursor Integration:
echo   - Open Cursor in WolfPackAI directory
echo   - Look for [🐺 N LLMs] in status bar
echo   - Use Auto mode with all models!
echo.
echo Press any key to close this window...
pause >nul
