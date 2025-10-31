# Configure Docker Desktop to start automatically

Write-Host "Configuring Docker Desktop auto-start..." -ForegroundColor Cyan

# Docker Desktop settings file location
$dockerSettingsPath = "$env:APPDATA\Docker\settings.json"

if (Test-Path $dockerSettingsPath) {
    $settings = Get-Content $dockerSettingsPath | ConvertFrom-Json
    $settings.autoStart = $true
    $settings | ConvertTo-Json -Depth 10 | Set-Content $dockerSettingsPath
    Write-Host "✅ Docker Desktop configured to auto-start!" -ForegroundColor Green
} else {
    Write-Host "⚠ Docker settings file not found at: $dockerSettingsPath" -ForegroundColor Yellow
    Write-Host "Manual setup:" -ForegroundColor Yellow
    Write-Host "1. Open Docker Desktop" -ForegroundColor White
    Write-Host "2. Settings > General" -ForegroundColor White
    Write-Host "3. Check 'Start Docker Desktop when you log in'" -ForegroundColor White
}
