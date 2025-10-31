# SSL Setup Quick Start

## Quick Setup Steps

1. **Configure Domain DNS**
   ```bash
   # Point your domain to your public IP
   # Example: litellm.yourdomain.com → 123.45.67.89
   ```

2. **Update appsettings.json**
   ```json
   {
     "LiteLLMSSL": {
       "enabled": true,
       "domain": "litellm.yourdomain.com",
       "email": "your-email@example.com",
       "useStaging": true,
       "httpPort": 80,
       "httpsPort": 443
     }
   }
   ```

3. **Open Firewall Ports**
   ```powershell
   # Windows PowerShell (as Admin)
   New-NetFirewallRule -DisplayName "HTTP" -Direction Inbound -LocalPort 80 -Protocol TCP -Action Allow
   New-NetFirewallRule -DisplayName "HTTPS" -Direction Inbound -LocalPort 443 -Protocol TCP -Action Allow
   ```

4. **Generate Temporary Certificates** (for initial startup)
   ```bash
   mkdir -p ./certs/live/litellm.yourdomain.com
   openssl req -x509 -nodes -days 1 -newkey rsa:2048 \
     -keyout ./certs/live/litellm.yourdomain.com/privkey.pem \
     -out ./certs/live/litellm.yourdomain.com/fullchain.pem \
     -subj "/CN=litellm.yourdomain.com"
   ```

5. **Run Application**
   ```bash
   dotnet run --project WolfPackAI.AppHost
   ```

6. **Verify SSL**
   ```bash
   curl -I https://litellm.yourdomain.com
   ```

## Files Created

- `WolfPackAI.AppBuilder/Configuration/ExtensionConfiguration.cs` - SSL config class
- `WolfPackAI.AppBuilder/Extensions/NginxExtensions.cs` - Nginx + Certbot extension
- `WolfPackAI.AppHost/nginx-litellm.conf` - Nginx configuration template
- `WolfPackAI.AppHost/nginx-entrypoint.sh` - Optional nginx entrypoint script
- `docs/SSL_SETUP.md` - Detailed setup guide
- `docs/CERTBOT_RENEWAL.md` - Certificate renewal guide

## Architecture

```
Internet (Port 80/443)
    ↓
nginx (Reverse Proxy)
    ↓
LiteLLM (Port 4000)
    ↑
certbot (Let's Encrypt)
```

## Next Steps

1. Test with staging certificates (`useStaging: true`)
2. Verify everything works
3. Switch to production (`useStaging: false`)
4. Set up certificate renewal (see `CERTBOT_RENEWAL.md`)

## Troubleshooting

- **Nginx won't start**: Check if certificates exist
- **Certbot fails**: Verify DNS and port 80 accessibility
- **SSL errors**: Ensure `useStaging: false` for production

See `SSL_SETUP.md` for detailed troubleshooting.

