# LiteLLM SSL Setup Guide

This guide explains how to expose LiteLLM on a public IP with SSL using nginx and certbot.

## Overview

The SSL setup uses:
- **nginx** as a reverse proxy for LiteLLM
- **certbot** for automatic SSL certificate management with Let's Encrypt
- Automatic HTTP to HTTPS redirection
- Secure headers and modern SSL/TLS configuration

## Prerequisites

1. **Domain Name**: You need a domain name pointing to your public IP
   - Example: `litellm.yourdomain.com`
   - DNS A record must point to your server's public IP

2. **Ports**: Ensure ports 80 and 443 are open on your firewall
   - Port 80: HTTP (for Let's Encrypt ACME challenges)
   - Port 443: HTTPS (for SSL traffic)

3. **Public IP**: Your server must be accessible from the internet

## Configuration

### Step 1: Update appsettings.json

Edit `WolfPackAI.AppHost/appsettings.json` and configure the `LiteLLMSSL` section:

```json
{
  "LiteLLMSSL": {
    "enabled": true,
    "domain": "litellm.yourdomain.com",
    "email": "your-email@example.com",
    "useStaging": false,
    "httpPort": 80,
    "httpsPort": 443
  }
}
```

**Configuration Options:**
- `enabled`: Set to `true` to enable SSL
- `domain`: Your domain name (must resolve to your public IP)
- `email`: Email for Let's Encrypt notifications
- `useStaging`: 
  - `true`: Use Let's Encrypt staging (for testing, no rate limits)
  - `false`: Use production Let's Encrypt (real certificates)
- `httpPort`: HTTP port (default: 80)
- `httpsPort`: HTTPS port (default: 443)

### Step 2: DNS Configuration

Ensure your domain DNS is configured correctly:

```bash
# Verify DNS resolution
nslookup litellm.yourdomain.com
# Should return your server's public IP
```

### Step 3: Firewall Configuration

Open the required ports:

**Windows (PowerShell as Administrator):**
```powershell
New-NetFirewallRule -DisplayName "HTTP" -Direction Inbound -LocalPort 80 -Protocol TCP -Action Allow
New-NetFirewallRule -DisplayName "HTTPS" -Direction Inbound -LocalPort 443 -Protocol TCP -Action Allow
```

**Linux (iptables):**
```bash
sudo iptables -A INPUT -p tcp --dport 80 -j ACCEPT
sudo iptables -A INPUT -p tcp --dport 443 -j ACCEPT
```

**Linux (ufw):**
```bash
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
```

## Deployment

### Initial Certificate Setup

**Important**: Nginx requires SSL certificates to start. Since certbot needs nginx running to obtain certificates, you have two options:

#### Option 1: Temporary Self-Signed Certificate (Recommended for Initial Setup)

Before starting the application, generate temporary self-signed certificates:

```bash
# Create certificate directory structure
mkdir -p ./certs/live/your-domain.com

# Generate temporary self-signed certificate
openssl req -x509 -nodes -days 1 -newkey rsa:2048 \
  -keyout ./certs/live/your-domain.com/privkey.pem \
  -out ./certs/live/your-domain.com/fullchain.pem \
  -subj "/CN=your-domain.com"

# Mount this directory as a volume (see docker-compose or modify Aspire config)
```

#### Option 2: Start Nginx Without SSL First

1. Temporarily comment out the HTTPS server block in `nginx-litellm.conf`
2. Start the application
3. Run certbot to obtain certificates
4. Uncomment HTTPS block and restart

### Initial Setup (Testing with Staging)

1. Set `useStaging: true` in `appsettings.json`
2. Generate temporary certificates (Option 1 above) or use Option 2
3. Run your application:
   ```bash
   dotnet run --project WolfPackAI.AppHost
   ```

4. The certbot container will automatically obtain a staging certificate
5. Access LiteLLM at `https://litellm.yourdomain.com`

### Production Deployment

1. **Test with staging first** to ensure everything works
2. Set `useStaging: false` in `appsettings.json`
3. Restart your application
4. Certbot will obtain a production certificate

## Certificate Renewal

Certbot certificates expire after 90 days. The setup includes automatic renewal:

- Certbot runs periodically to check and renew certificates
- Nginx automatically reloads when certificates are updated
- No downtime required for renewal

## Architecture

```
Internet → nginx (Port 80/443) → LiteLLM (Port 4000)
           ↓
        certbot (Let's Encrypt)
```

**Container Flow:**
1. Nginx starts and listens on ports 80/443
2. Certbot obtains SSL certificate from Let's Encrypt
3. Nginx reloads with SSL configuration
4. All HTTP traffic redirects to HTTPS
5. HTTPS traffic proxies to LiteLLM

## Troubleshooting

### Certificate Not Obtained

**Problem**: Certbot fails to obtain certificate

**Solutions**:
1. Verify DNS points to your IP:
   ```bash
   nslookup your-domain.com
   ```
2. Check port 80 is accessible:
   ```bash
   curl -I http://your-domain.com
   ```
3. Check certbot logs:
   ```bash
   docker logs nginx-litellm-certbot
   ```

### Nginx Won't Start

**Problem**: Nginx fails to start

**Solutions**:
1. Check nginx configuration:
   ```bash
   docker exec nginx-litellm nginx -t
   ```
2. Verify domain substitution in config
3. Check port conflicts

### SSL Certificate Errors

**Problem**: Browser shows certificate errors

**Solutions**:
1. If using staging, browser will show warnings (expected)
2. Ensure `useStaging: false` for production
3. Verify certificate exists:
   ```bash
   docker exec nginx-litellm ls -la /etc/letsencrypt/live/your-domain.com/
   ```

### Rate Limiting

**Problem**: Let's Encrypt rate limits exceeded

**Solutions**:
1. Use staging environment for testing (`useStaging: true`)
2. Wait for rate limit reset (usually 1 week)
3. Use different domain for testing

## Security Considerations

1. **Firewall**: Only expose ports 80 and 443
2. **Master Key**: Keep LiteLLM master key secure
3. **Email**: Use valid email for Let's Encrypt notifications
4. **Staging**: Always test with staging before production
5. **Renewal**: Monitor certificate expiration (auto-renewal included)

## Advanced Configuration

### Custom SSL Configuration

Edit `nginx-litellm.conf` to customize:
- SSL protocols and ciphers
- Security headers
- Proxy timeouts
- Request size limits

### Multiple Domains

To support multiple domains, modify the configuration:
1. Update `domain` to include multiple domains (comma-separated)
2. Update certbot args in `NginxExtensions.cs`
3. Update nginx config with multiple `server_name` entries

## Monitoring

Monitor certificate expiration:
```bash
docker exec nginx-litellm certbot certificates
```

Check nginx status:
```bash
docker exec nginx-litellm nginx -t
docker logs nginx-litellm
```

## Support

For issues:
1. Check container logs
2. Verify DNS configuration
3. Ensure ports are open
4. Review Let's Encrypt rate limits

