# Create desktop shortcuts for easy access

$WshShell = New-Object -comObject WScript.Shell
$Desktop = [System.Environment]::GetFolderPath("Desktop")

Write-Host "Creating desktop shortcuts..." -ForegroundColor Cyan
Write-Host ""

# Shortcut 1: OpenWebUI (Main AI Chat Interface)
$Shortcut1 = $WshShell.CreateShortcut("$Desktop\WolfPackAI - Chat.url")
$Shortcut1.TargetPath = "http://localhost:5000/chat"
$Shortcut1.Save()
Write-Host "✅ Created: WolfPackAI - Chat.url" -ForegroundColor Green

# Shortcut 2: Gatekeeper API Documentation
$Shortcut2 = $WshShell.CreateShortcut("$Desktop\WolfPackAI - API Docs.url")
$Shortcut2.TargetPath = "http://localhost:7000/swagger"
$Shortcut2.Save()
Write-Host "✅ Created: WolfPackAI - API Docs.url" -ForegroundColor Green

# Shortcut 3: Start WolfPackAI (manual start if needed)
$Shortcut3 = $WshShell.CreateShortcut("$Desktop\Start WolfPackAI.lnk")
$Shortcut3.TargetPath = "C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI\start-wolfpack-complete.bat"
$Shortcut3.WorkingDirectory = "C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI"
$Shortcut3.Description = "Start WolfPackAI, Docker, Cursor, PowerShell, Terminal"
$Shortcut3.Save()
Write-Host "✅ Created: Start WolfPackAI.lnk" -ForegroundColor Green

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Desktop Shortcuts Created!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "On your desktop you now have:" -ForegroundColor Yellow
Write-Host "  1. WolfPackAI - Chat → Opens OpenWebUI chat interface" -ForegroundColor White
Write-Host "  2. WolfPackAI - API Docs → Opens gatekeeper API documentation" -ForegroundColor White
Write-Host "  3. Start WolfPackAI → Manually start all services (if needed)" -ForegroundColor White
Write-Host ""
Write-Host "Services auto-start on boot, so you can just click 'WolfPackAI - Chat' after booting!" -ForegroundColor Cyan
