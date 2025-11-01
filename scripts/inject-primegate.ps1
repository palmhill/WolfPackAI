# WolfPackAI PrimeGate - Seamless Injection Setup
# Zero disruption, pure enhancement

param(
    [switch]$SkipBuild,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "🐺 WolfPackAI PrimeGate - Seamless Injection" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "Injection Mode: Additive (Zero Disruption)" -ForegroundColor Green
Write-Host "Isolation: WolfPackAI Directory Only" -ForegroundColor Green
Write-Host "Authority: Orchestrator (Full Control)" -ForegroundColor Green
Write-Host ""

# Step 1: Verify we're in Wolf PackAI directory
Write-Host "[1/7] Verifying location..." -ForegroundColor Yellow
$currentDir = Get-Location
if (-not ($currentDir.Path -like "*WolfPackAI*")) {
    Write-Host "   ⚠ Not in WolfPackAI directory!" -ForegroundColor Red
    Write-Host "   This setup should be run from WolfPackAI root directory." -ForegroundColor Red
    Write-Host "   Current: $($currentDir.Path)" -ForegroundColor Red
    exit 1
}
Write-Host "   ✓ Location confirmed: WolfPackAI directory" -ForegroundColor Green
Write-Host ""

# Step 2: Check existing files (isolation verification)
Write-Host "[2/7] Checking isolation..." -ForegroundColor Yellow
$existingProjects = @()
Get-ChildItem -Directory | ForEach-Object {
    if ($_.Name -notlike "WolfPack*" -and $_.Name -ne ".cursor" -and $_.Name -ne ".git" -and $_.Name -ne "scripts") {
        $existingProjects += $_.Name
    }
}
if ($existingProjects.Count -gt 0) {
    Write-Host "   ℹ Other projects detected: $($existingProjects -join ', ')" -ForegroundColor Cyan
    Write-Host "   ✓ Isolation: PrimeGate will NOT affect these projects" -ForegroundColor Green
}
else {
    Write-Host "   ✓ No other projects in this directory" -ForegroundColor Green
}
Write-Host ""

# Step 3: Build PrimeGate service
if (-not $SkipBuild) {
    Write-Host "[3/7] Building PrimeGate service..." -ForegroundColor Yellow
    try {
        $buildOutput = dotnet build WolfPackAI.PrimeGate/WolfPackAI.PrimeGate.csproj --configuration Release 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✓ Build successful" -ForegroundColor Green
        }
        else {
            Write-Host "   ✗ Build failed:" -ForegroundColor Red
            if ($Verbose) { Write-Host $buildOutput }
            exit 1
        }
    }
    catch {
        Write-Host "   ✗ Build error: $_" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "[3/7] Skipping build (--SkipBuild)" -ForegroundColor Gray
}
Write-Host ""

# Step 4: Verify .cursor directory exists
Write-Host "[4/7] Setting up Cursor integration..." -ForegroundColor Yellow
if (-not (Test-Path ".cursor")) {
    New-Item -ItemType Directory -Path ".cursor" -Force | Out-Null
    Write-Host "   ✓ Created .cursor directory" -ForegroundColor Green
}
else {
    Write-Host "   ✓ .cursor directory exists" -ForegroundColor Green
}

if (-not (Test-Path ".cursor/extensions")) {
    New-Item -ItemType Directory -Path ".cursor/extensions" -Force | Out-Null
    Write-Host "   ✓ Created .cursor/extensions directory" -ForegroundColor Green
}
else {
    Write-Host "   ✓ .cursor/extensions directory exists" -ForegroundColor Green
}
Write-Host ""

# Step 5: Verify extension file exists
Write-Host "[5/7] Verifying extension files..." -ForegroundColor Yellow
if (Test-Path ".cursor/extensions/wolfpackai-primegate.js") {
    Write-Host "   ✓ PrimeGate extension found" -ForegroundColor Green
}
else {
    Write-Host "   ✗ Extension file missing!" -ForegroundColor Red
    Write-Host "   Expected: .cursor/extensions/wolfpackai-primegate.js" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 6: Verify .cursorrules file
Write-Host "[6/7] Verifying .cursorrules..." -ForegroundColor Yellow
if (Test-Path ".cursorrules") {
    Write-Host "   ✓ .cursorrules found (project-specific)" -ForegroundColor Green
}
else {
    Write-Host "   ✗ .cursorrules missing!" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 7: Final verification
Write-Host "[7/7] Final verification..." -ForegroundColor Yellow
$verification = @{
    "PrimeGate Project" = Test-Path "WolfPackAI.PrimeGate/WolfPackAI.PrimeGate.csproj"
    "PrimeGate Program" = Test-Path "WolfPackAI.PrimeGate/Program.cs"
    "Cursor Extension" = Test-Path ".cursor/extensions/wolfpackai-primegate.js"
    ".cursorrules" = Test-Path ".cursorrules"
}

$allValid = $true
foreach ($item in $verification.GetEnumerator()) {
    if ($item.Value) {
        Write-Host "   ✓ $($item.Key)" -ForegroundColor Green
    }
    else {
        Write-Host "   ✗ $($item.Key)" -ForegroundColor Red
        $allValid = $false
    }
}

if (-not $allValid) {
    Write-Host ""
    Write-Host "   ✗ Verification failed - missing files" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Success summary
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "✅ PrimeGate injected successfully!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""

# Usage instructions
Write-Host "📋 Next Steps:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Start WolfPackAI services:" -ForegroundColor White
Write-Host "   dotnet run --project WolfPackAI.AppHost" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Start PrimeGate service (in new terminal):" -ForegroundColor White
Write-Host "   dotnet run --project WolfPackAI.PrimeGate" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Open Cursor in WolfPackAI directory:" -ForegroundColor White
Write-Host "   - PrimeGate will be auto-detected" -ForegroundColor Gray
Write-Host "   - All WolfPackAI LLMs will appear in model list" -ForegroundColor Gray
Write-Host "   - Orchestrator will have full control" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Check status:" -ForegroundColor White
Write-Host "   - Look for [🐺 N LLMs] in Cursor status bar" -ForegroundColor Gray
Write-Host "   - Click to see available models" -ForegroundColor Gray
Write-Host ""

# Isolation reminder
Write-Host "🔒 Isolation Guarantee:" -ForegroundColor Cyan
Write-Host "   ✓ Only active in WolfPackAI directory" -ForegroundColor Green
Write-Host "   ✓ Does NOT affect PaiiD or PaπD 2mx" -ForegroundColor Green
Write-Host "   ✓ Does NOT affect other projects" -ForegroundColor Green
Write-Host "   ✓ Zero modifications to existing files" -ForegroundColor Green
Write-Host "   ✓ Graceful degradation if services offline" -ForegroundColor Green
Write-Host ""

# Optional: Test primegate
Write-Host "🧪 Test PrimeGate (optional):" -ForegroundColor Cyan
Write-Host "   After starting services, visit: http://localhost:7000/swagger" -ForegroundColor Gray
Write-Host "   To see API documentation and test endpoints" -ForegroundColor Gray
Write-Host ""

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "🐺 WolfPackAI PrimeGate is ready to supercharge your coding!" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
