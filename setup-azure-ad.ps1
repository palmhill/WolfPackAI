# setup-azure-ad.ps1

$tenantId = "your-tenant-id"
$appName = "OpenWebUI-LiteLLM-Portal"

# Create App Registration
$app = New-AzADApplication -DisplayName $appName `
  -IdentifierUris "api://$appName" `
  -ReplyUrls @(
    "http://localhost:8080/oauth/callback",
    "https://your-domain.com/oauth/callback"
  )

# Create Client Secret
$secret = New-AzADAppCredential -ApplicationId $app.AppId

# Create Service Principal
New-AzADServicePrincipal -ApplicationId $app.AppId

# Add Required API Permissions
Add-AzADAppPermission -ApplicationId $app.AppId `
  -ApiId "00000003-0000-0000-c000-000000000000" `  # Microsoft Graph
  -PermissionId "e1fe6dd8-ba31-4d61-89e7-88639da4683d" `  # User.Read
  -Type "Scope"

Write-Host "App Registration Created:"
Write-Host "Client ID: $($app.AppId)"
Write-Host "Client Secret: $($secret.SecretText)"
Write-Host "Tenant ID: $tenantId"