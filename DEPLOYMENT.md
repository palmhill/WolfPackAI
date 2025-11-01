# WolfPackAI Deployment Guide

Complete production deployment guide for WolfPackAI AI Platform.

## 🚀 Deployment Options

### Option 1: Local Development (Aspire)
**Best for**: Development, testing, local AI workloads
**Requirements**: .NET 9.0 SDK, Docker Desktop, 16GB RAM

```bash
# One-command launch
.\bootstrap.ps1

# Manual launch
dotnet run --project WolfPackAI.AppHost
```

**Access**:
- Dashboard: http://localhost
- Aspire Dashboard: http://localhost:15021

---

### Option 2: Docker Compose (Production)
**Best for**: Single-server production, small teams
**Requirements**: Docker Engine 20.10+, 32GB RAM, GPU (optional)

```bash
cd deploy/docker

# Configure environment
cp .env.production.example .env.production
nano .env.production  # Update all CHANGE_ME values

# Deploy
docker-compose -f docker-compose.production.yml --env-file .env.production up -d

# Initialize Ollama model
docker exec wolfpackai-ollama ollama pull deepseek-coder-v2:16b
```

**Access**:
- Dashboard: http://your-server-ip
- Services: http://your-server-ip/[chat|litellm|n8n]

---

### Option 3: Kubernetes (Enterprise)
**Best for**: Multi-node clusters, high availability, scaling
**Requirements**: Kubernetes 1.24+, kubectl, Persistent Volume provisioner

```bash
cd deploy/kubernetes

# Update secrets
nano secrets.yaml  # Replace all CHANGE_ME values
kubectl apply -f secrets.yaml

# Deploy all services
kubectl apply -f namespace.yaml
kubectl apply -f postgres-statefulset.yaml
kubectl apply -f ollama-deployment.yaml
kubectl apply -f litellm-deployment.yaml
kubectl apply -f openwebui-deployment.yaml
kubectl apply -f n8n-deployment.yaml
kubectl apply -f dashboard-deployment.yaml

# Get LoadBalancer IP
kubectl get svc dashboard -n wolfpackai
```

**Access**:
- Dashboard: http://<EXTERNAL-IP>
- Scale: `kubectl scale deployment/openwebui --replicas=5 -n wolfpackai`

---

## 📋 Prerequisites

### All Deployments
- [x] 16GB RAM minimum (32GB recommended)
- [x] 100GB disk space
- [x] Modern CPU (4+ cores)
- [x] NVIDIA GPU (optional, for Ollama acceleration)

### Development
- [x] .NET 9.0 SDK
- [x] Docker Desktop
- [x] Aspire workload: `dotnet workload install aspire`
- [x] Python 3.9+ (for MOD SQUAD)
- [x] Node.js 22+ (for MOD SQUAD)

### Production
- [x] Docker Engine or Kubernetes
- [x] Valid SSL certificates (for HTTPS)
- [x] Domain name (recommended)
- [x] Backup strategy

---

## 🔐 Security Configuration

### 1. Change Default Secrets

**Critical - Do this first!**

```bash
# Generate secure passwords
openssl rand -base64 32  # PostgreSQL password
openssl rand -base64 32  # LiteLLM master key
openssl rand -base64 32  # n8n auth password

# Update .env.production or secrets.yaml
POSTGRES_PASSWORD=your-generated-password
LITELLM_MASTER_KEY=sk-your-generated-key
N8N_BASIC_AUTH_PASSWORD=your-generated-password
```

### 2. Enable HTTPS/TLS

**Docker Compose**: Add Caddy or Nginx reverse proxy
**Kubernetes**: Use Ingress with cert-manager

```yaml
# Example Kubernetes Ingress
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: wolfpackai-ingress
  annotations:
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
spec:
  tls:
  - hosts:
    - wolfpackai.yourdomain.com
    secretName: wolfpackai-tls
  rules:
  - host: wolfpackai.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: dashboard
            port:
              number: 80
```

### 3. Restrict Network Access

```bash
# Firewall rules (ufw example)
ufw allow 80/tcp
ufw allow 443/tcp
ufw deny 5432/tcp  # Block direct PostgreSQL access
ufw enable
```

### 4. Configure CORS

Edit `WolfPackAI.AppHost/appsettings.json`:

```json
{
  "OpenWebUI": {
    "PublicUrl": "https://yourdomain.com/chat",
    "CorsAllowOrigin": "https://yourdomain.com"
  }
}
```

---

## 🎯 Post-Deployment Steps

### 1. Verify All Services

```bash
# Check service health
curl http://localhost/health          # Dashboard
curl http://localhost/chat/health     # OpenWebUI
curl http://localhost/litellm/health  # LiteLLM
curl http://localhost:5678/healthz    # n8n

# Or use MOD SQUAD
npm run mod:health
```

### 2. Create First Admin User

1. Navigate to http://your-domain/chat
2. Click "Sign Up"
3. Create admin account (first user is admin)
4. Configure AI models in settings

### 3. Configure n8n Workflows

1. Access n8n at http://your-domain:5678
2. Login with credentials from .env
3. Import workflow templates from `workflows/n8n-templates/`
4. Activate workflows

### 4. Set Up Monitoring

```bash
# If using Kubernetes
kubectl apply -f monitoring/prometheus.yml
kubectl apply -f monitoring/grafana.yml

# Access Grafana
kubectl port-forward svc/grafana 3000:3000 -n wolfpackai
# Open http://localhost:3000 (admin/admin)
```

### 5. Configure Backups

```bash
# Daily PostgreSQL backup (cron)
0 2 * * * docker exec wolfpackai-postgres pg_dumpall -U postgres > /backups/postgres-$(date +\%Y\%m\%d).sql

# Weekly volume backup
0 3 * * 0 docker run --rm -v wolfpackai_ollama_models:/data -v /backups:/backup alpine tar czf /backup/ollama-$(date +\%Y\%m\%d).tar.gz -C /data .
```

---

## 📊 Monitoring & Observability

### Aspire Dashboard
- **URL**: http://localhost:15021
- **Features**: Real-time logs, metrics, traces
- **Dev only**: Not available in pure Docker/K8s

### Prometheus + Grafana
```bash
# View pre-configured dashboard
http://localhost:3000/d/wolfpackai-overview
```

**Key Metrics**:
- Service health (up/down)
- Request rate per service
- AI token usage
- Response latency (P95/P99)
- Error rates

### MOD SQUAD Validation
```bash
# Run full validation suite
npm run mod:all

# Individual checks
npm run mod:health   # Health checks
npm run mod:browser  # UI tests
npm run mod:audit    # Security audit
```

---

## 🔧 Troubleshooting

### Services Won't Start

**Symptoms**: Containers constantly restarting
**Diagnosis**:
```bash
# Check logs
docker logs wolfpackai-<service>

# Kubernetes
kubectl logs deployment/<service> -n wolfpackai

# Check dependencies
docker ps  # All services should show "healthy"
```

**Common Fixes**:
- Ensure PostgreSQL is ready before dependent services
- Verify environment variables are set
- Check port conflicts: `netstat -tuln | grep <port>`
- Increase Docker memory limit (Settings → Resources)

### Cannot Access Services

**Symptoms**: 502 Bad Gateway, Connection Refused
**Diagnosis**:
```bash
# Test internal connectivity
docker exec wolfpackai-dashboard curl http://openwebui:8080/health

# Check routing
docker logs wolfpackai-dashboard | grep "upstream"
```

**Common Fixes**:
- Verify reverse proxy configuration
- Check firewall rules
- Ensure services are on same Docker network
- Test with `curl -v http://localhost/health`

### AI Responses Are Slow

**Symptoms**: Long wait times for AI responses
**Diagnosis**:
```bash
# Check Ollama GPU usage
docker exec wolfpackai-ollama nvidia-smi

# Check model is loaded
docker exec wolfpackai-ollama ollama list
```

**Common Fixes**:
- Enable GPU support in Docker runtime
- Increase container resource limits
- Use smaller/faster model (e.g., mistral:7b)
- Add more Ollama replicas

### High Memory Usage

**Symptoms**: System OOM, containers killed
**Diagnosis**:
```bash
docker stats  # Check resource usage
```

**Fixes**:
- Set resource limits in docker-compose.yml:
```yaml
services:
  ollama:
    deploy:
      resources:
        limits:
          memory: 8G
```
- Use smaller Ollama models
- Reduce concurrent request limits

---

## 🎓 Best Practices

### Development
- [x] Use `bootstrap.ps1` for one-command setup
- [x] Run MOD SQUAD before committing: `npm run mod:all`
- [x] Monitor Aspire Dashboard during development
- [x] Use `.env` files for local configuration

### Staging
- [x] Deploy via Docker Compose on dedicated server
- [x] Enable basic monitoring (health checks)
- [x] Test with production-like data volumes
- [x] Verify backup/restore procedures

### Production
- [x] Use Kubernetes for high availability
- [x] Enable comprehensive monitoring (Prometheus/Grafana)
- [x] Implement automated backups
- [x] Use secrets manager (Azure Key Vault, AWS Secrets Manager)
- [x] Enable HTTPS/TLS everywhere
- [x] Configure alerting (PagerDuty, Slack)
- [x] Set up log aggregation (ELK, Loki)
- [x] Document runbooks for common issues
- [x] Test disaster recovery quarterly

---

## 📈 Scaling Guide

### Horizontal Scaling

**Docker Compose**:
```bash
docker-compose up -d --scale openwebui=3 --scale litellm=2
```

**Kubernetes**:
```bash
kubectl scale deployment/openwebui --replicas=5 -n wolfpackai
kubectl scale deployment/litellm --replicas=3 -n wolfpackai
```

### Vertical Scaling

Increase resources per container:
```yaml
resources:
  limits:
    cpus: '4'
    memory: 8G
  requests:
    cpus: '2'
    memory: 4G
```

### Auto-Scaling (Kubernetes)

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: openwebui-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: openwebui
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
```

---

## 🆘 Support & Resources

### Documentation
- [README.md](README.md) - Project overview
- [CLAUDE.md](CLAUDE.md) - Architecture details
- [workflows/README.md](workflows/README.md) - n8n workflow templates
- [monitoring/README.md](monitoring/README.md) - Observability setup

### Quick Links
- [.NET Aspire Docs](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Docker Compose Reference](https://docs.docker.com/compose/)
- [Kubernetes Docs](https://kubernetes.io/docs/)
- [MOD SQUAD Guide](docs/mod-squad/QUICKSTART.md)

### Getting Help
- **GitHub Issues**: Report bugs and request features
- **Discussions**: Community Q&A (coming soon)
- **Enterprise Support**: Contact for dedicated support

---

## ✅ Deployment Checklist

### Pre-Deployment
- [ ] Review system requirements
- [ ] Generate secure passwords
- [ ] Configure domain/DNS
- [ ] Obtain SSL certificates
- [ ] Set up backup destination

### Deployment
- [ ] Deploy infrastructure (K8s cluster or Docker host)
- [ ] Apply secrets/configuration
- [ ] Deploy database (PostgreSQL)
- [ ] Deploy services (Ollama, LiteLLM, OpenWebUI, n8n)
- [ ] Deploy reverse proxy/ingress
- [ ] Verify all health checks pass

### Post-Deployment
- [ ] Create admin user
- [ ] Configure AI models
- [ ] Import n8n workflows
- [ ] Set up monitoring dashboards
- [ ] Configure automated backups
- [ ] Test all service endpoints
- [ ] Load test (recommended)
- [ ] Document any customizations

### Ongoing
- [ ] Monitor service health daily
- [ ] Review logs weekly
- [ ] Test backups monthly
- [ ] Update services quarterly
- [ ] Review security annually

---

**Your WolfPackAI platform is now production-ready!** 🐺🚀

For additional help, see the [troubleshooting section](#-troubleshooting) or open an issue on GitHub.
