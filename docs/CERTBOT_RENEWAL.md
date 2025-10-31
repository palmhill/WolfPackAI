# Certbot Certificate Renewal Setup

## Overview

Let's Encrypt certificates expire after 90 days. This guide explains how to set up automatic renewal.

## Automatic Renewal

### Option 1: Manual Renewal (Simple)

Run certbot renewal manually:

```bash
# Enter the certbot container
docker exec -it nginx-litellm-certbot certbot renew

# Reload nginx after renewal
docker exec nginx-litellm nginx -s reload
```

### Option 2: Scheduled Renewal Container

Create a renewal container that runs periodically:

**Add to Program.cs:**
```csharp
// Certificate renewal container (runs daily)
var certbotRenewal = builder.AddContainer("certbot-renewal", "certbot/certbot")
    .WithVolume("litellm-certs", "/etc/letsencrypt")
    .WithVolume("certbot-webroot", "/var/www/certbot")
    .WithArgs("renew", "--quiet", "--deploy-hook", "nginx -s reload")
    .WithEnvironment("RENEWAL_SCHEDULE", "0 2 * * *"); // Daily at 2 AM
```

### Option 3: System Cron Job

Set up a cron job on the host:

```bash
# Edit crontab
crontab -e

# Add renewal job (runs twice daily)
0 0,12 * * * docker exec nginx-litellm-certbot certbot renew --quiet && docker exec nginx-litellm nginx -s reload
```

### Option 4: Docker Compose with Init Container

If using docker-compose directly:

```yaml
certbot-renewal:
  image: certbot/certbot
  volumes:
    - litellm-certs:/etc/letsencrypt
    - certbot-webroot:/var/www/certbot
  command: sh -c "while :; do sleep 12h & wait $${!}; certbot renew; done"
  restart: unless-stopped
```

## Testing Renewal

Test renewal without actually renewing:

```bash
docker exec nginx-litellm-certbot certbot renew --dry-run
```

## Verification

Check certificate expiration:

```bash
docker exec nginx-litellm-certbot certbot certificates
```

## Troubleshooting

### Renewal Fails

1. **Check logs:**
   ```bash
   docker logs nginx-litellm-certbot
   ```

2. **Verify nginx is running:**
   ```bash
   docker ps | grep nginx-litellm
   ```

3. **Check port 80 accessibility:**
   ```bash
   curl -I http://your-domain.com/.well-known/acme-challenge/test
   ```

### Nginx Won't Reload

If nginx reload fails after renewal:

```bash
# Check nginx config
docker exec nginx-litellm nginx -t

# Manual reload
docker exec nginx-litellm nginx -s reload

# Or restart container
docker restart nginx-litellm
```

## Best Practices

1. **Monitor expiration**: Set up alerts for certificates expiring in < 30 days
2. **Test renewal**: Use `--dry-run` regularly to verify renewal works
3. **Backup certificates**: Backup `/etc/letsencrypt` directory regularly
4. **Multiple renewals**: Run renewal more frequently than needed (e.g., daily)

## Integration with Monitoring

Set up monitoring to alert on certificate expiration:

```bash
# Check days until expiration
docker exec nginx-litellm-certbot certbot certificates | grep "Expiry Date"
```

