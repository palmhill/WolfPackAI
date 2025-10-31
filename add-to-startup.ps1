# Add WolfPackAI to Windows Startup

$WshShell = New-Object -comObject WScript.Shell
$StartupFolder = "C:\Users\SSaint-Cyr\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup"
$ShortcutPath = Join-Path $StartupFolder "WolfPackAI-AutoStart.lnk"
$TargetPath = "C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI\start-wolfpack-complete.bat"

$Shortcut = $WshShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = $TargetPath
$Shortcut.WorkingDirectory = "C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI"
$Shortcut.Description = "Auto-start WolfPackAI services, Docker, Cursor, PowerShell, Terminal"
$Shortcut.Save()

Write-Host "✅ Added to Windows Startup!" -ForegroundColor Green
Write-Host "Location: $ShortcutPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next boot will automatically start:" -ForegroundColor Yellow
Write-Host "  - Docker Desktop" -ForegroundColor White
Write-Host "  - WolfPackAI Services" -ForegroundColor White
Write-Host "  - Gatekeeper" -ForegroundColor White
Write-Host "  - PowerShell (WolfPackAI directory)" -ForegroundColor White
Write-Host "  - Windows Terminal (WolfPackAI directory)" -ForegroundColor White
Write-Host "  - Cursor IDE (WolfPackAI project)" -ForegroundColor White
