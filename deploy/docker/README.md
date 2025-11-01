# Docker Compose Production Deployment

## Quick Start

### 1. Prerequisites

- Docker Engine 20.10+ with Docker Compose V2
- NVIDIA Docker runtime (for GPU support)
- 16GB RAM minimum (32GB recommended)
- 100GB disk space

### 2. Configuration

```bash
# Copy environment template
cp .env.production.example .env.production

# Edit with your secure values
nano .env.production

# IMPORTANT: Update all CHANGE_ME values!
```

### 3. Build Dashboard Image

```bash
cd ../..
docker build -f WolfPackAI.Dashboard/Dockerfile -t wolfpackai-dashboard:latest .
```

### 4. Deploy

```bash
cd deploy/docker

# Start all services
docker-compose -f docker-compose.production.yml --env-file .env.production up -d

# View logs
docker-compose -f docker-compose.production.yml logs -f

# Check health
docker-compose -f docker-compose.production.yml ps
```

### 5. Initialize Ollama Model

```bash
# Download the model (this takes 5-10 minutes)
docker exec wolfpackai-ollama ollama pull deepseek-coder-v2:16b

# Verify model is available
docker exec wolfpackai-ollama ollama list
```

### 6. Access Services

- **Dashboard**: http://localhost (or your domain)
- **OpenWebUI**: http://localhost/chat
- **LiteLLM**: http://localhost/litellm
- **n8n**: http://localhost:5678 (direct access)
- **Aspire Dashboard**: Not available in pure Docker deployment

## Management Commands

### Start Services

```bash
docker-compose -f docker-compose.production.yml --env-file .env.production up -d
```

### Stop Services

```bash
docker-compose -f docker-compose.production.yml down
```

### Restart a Service

```bash
docker-compose -f docker-compose.production.yml restart openwebui
```

### View Logs

```bash
# All services
docker-compose -f docker-compose.production.yml logs -f

# Specific service
docker-compose -f docker-compose.production.yml logs -f openwebui

# Last 100 lines
docker-compose -f docker-compose.production.yml logs --tail=100 litellm
```

### Check Service Health

```bash
docker-compose -f docker-compose.production.yml ps

# Or use health check script
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

## Backup & Restore

### Backup All Data

```bash
# Create backup directory
mkdir -p backups

# Backup PostgreSQL
docker exec wolfpackai-postgres pg_dumpall -U postgres > backups/postgres-$(date +%Y%m%d-%H%M%S).sql

# Backup volumes
docker run --rm -v wolfpackai_ollama_models:/data -v $(pwd)/backups:/backup alpine tar czf /backup/ollama-models-$(date +%Y%m%d-%H%M%S).tar.gz -C /data .
docker run --rm -v wolfpackai_n8n_data:/data -v $(pwd)/backups:/backup alpine tar czf /backup/n8n-data-$(date +%Y%m%d-%H%M%S).tar.gz -C /data .
docker run --rm -v wolfpackai_openwebui_data:/data -v $(pwd)/backups:/backup alpine tar czf /backup/openwebui-data-$(date +%Y%m%d-%H%M%S).tar.gz -C /data .
```

### Restore PostgreSQL

```bash
# Stop services
docker-compose -f docker-compose.production.yml down

# Start only postgres
docker-compose -f docker-compose.production.yml up -d postgres

# Restore database
cat backups/postgres-YYYYMMDD-HHMMSS.sql | docker exec -i wolfpackai-postgres psql -U postgres

# Start all services
docker-compose -f docker-compose.production.yml up -d
```

## Monitoring

### Resource Usage

```bash
# Monitor container resource usage
docker stats

# Check disk usage
docker system df
```

### Health Checks

```bash
# PostgreSQL
docker exec wolfpackai-postgres pg_isready -U postgres

# Ollama
curl http://localhost:1143/api/tags

# LiteLLM
curl http://localhost:4000/health

# OpenWebUI
curl http://localhost:8080/health

# n8n
curl http://localhost:5678/healthz
```

## Troubleshooting

### Services Won't Start

```bash
# Check logs for errors
docker-compose -f docker-compose.production.yml logs

# Verify environment variables
docker-compose -f docker-compose.production.yml config

# Check network connectivity
docker network inspect wolfpackai_wolfpackai-network
```

### PostgreSQL Connection Issues

```bash
# Check if PostgreSQL is ready
docker exec wolfpackai-postgres pg_isready -U postgres

# Verify databases exist
docker exec wolfpackai-postgres psql -U postgres -c "\l"

# Check connections
docker exec wolfpackai-postgres psql -U postgres -c "SELECT * FROM pg_stat_activity;"
```

### GPU Not Available

```bash
# Verify NVIDIA Docker runtime
docker run --rm --gpus all nvidia/cuda:11.8.0-base-ubuntu22.04 nvidia-smi

# Check Ollama can see GPU
docker exec wolfpackai-ollama nvidia-smi
```

### Dashboard Returns 502

```bash
# Check if upstream services are healthy
docker-compose -f docker-compose.production.yml ps

# Check dashboard logs
docker logs wolfpackai-dashboard

# Test connectivity from dashboard container
docker exec wolfpackai-dashboard wget -O- http://openwebui:8080/health
```

## Security Hardening

### 1. Use Strong Passwords

- Update all CHANGE_ME values in .env.production
- Use password manager to generate secure passwords
- Rotate passwords regularly

### 2. Enable TLS/SSL

```bash
# Install Caddy or Nginx reverse proxy
docker run -d \
  --name caddy \
  -p 80:80 \
  -p 443:443 \
  -v caddy_data:/data \
  -v caddy_config:/config \
  caddy:latest \
  caddy reverse-proxy --from yourdomain.com --to localhost:80
```

### 3. Restrict Network Access

```yaml
# Add to docker-compose.production.yml
networks:
  wolfpackai-network:
    driver: bridge
    internal: true  # Isolate from external access

  public-network:
    driver: bridge
```

### 4. Enable Firewall

```bash
# Allow only necessary ports
ufw allow 80/tcp
ufw allow 443/tcp
ufw enable
```

## Scaling

### Horizontal Scaling

```bash
# Scale OpenWebUI to 3 replicas
docker-compose -f docker-compose.production.yml up -d --scale openwebui=3

# Scale LiteLLM to 2 replicas
docker-compose -f docker-compose.production.yml up -d --scale litellm=2
```

### Resource Limits

Add to docker-compose.production.yml:

```yaml
services:
  openwebui:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 4G
        reservations:
          cpus: '1'
          memory: 2G
```

## Updating

### Update Images

```bash
# Pull latest images
docker-compose -f docker-compose.production.yml pull

# Recreate containers with new images
docker-compose -f docker-compose.production.yml up -d

# Remove old images
docker image prune -a
```

### Update Dashboard

```bash
# Rebuild dashboard image
cd ../..
docker build -f WolfPackAI.Dashboard/Dockerfile -t wolfpackai-dashboard:latest .

# Restart dashboard service
cd deploy/docker
docker-compose -f docker-compose.production.yml up -d dashboard
```

## Complete Cleanup

```bash
# Stop and remove all containers
docker-compose -f docker-compose.production.yml down

# Remove volumes (WARNING: This deletes all data!)
docker-compose -f docker-compose.production.yml down -v

# Remove images
docker rmi wolfpackai-dashboard:latest
docker rmi ghcr.io/open-webui/open-webui:latest
docker rmi ghcr.io/berriai/litellm-database:main-v1.74.8-nightly
docker rmi docker.n8n.io/n8nio/n8n:latest
docker rmi ollama/ollama:latest
docker rmi postgres:16
```

## Next Steps

- Set up automated backups (cron jobs)
- Configure external monitoring (Prometheus/Grafana)
- Implement log aggregation (ELK/Loki)
- Set up CI/CD pipeline
- Configure high availability
