# Kubernetes Deployment Guide for WolfPackAI

## Prerequisites

- Kubernetes cluster (1.24+)
- kubectl configured
- Persistent Volume provisioner
- Optional: GPU support for Ollama

## Quick Start

### 1. Create Namespace and Secrets

```bash
# Create namespace
kubectl apply -f namespace.yaml

# Update secrets.yaml with your actual secrets
# IMPORTANT: Replace all CHANGE_ME_IN_PRODUCTION values
kubectl apply -f secrets.yaml
```

### 2. Deploy PostgreSQL

```bash
kubectl apply -f postgres-statefulset.yaml

# Wait for PostgreSQL to be ready
kubectl wait --for=condition=ready pod -l app=postgres -n wolfpackai --timeout=300s

# Create databases
kubectl exec -it postgres-0 -n wolfpackai -- psql -U postgres -c "CREATE DATABASE openwebuidb;"
kubectl exec -it postgres-0 -n wolfpackai -- psql -U postgres -c "CREATE DATABASE litellmdb;"
kubectl exec -it postgres-0 -n wolfpackai -- psql -U postgres -c "CREATE DATABASE n8ndb;"
```

### 3. Deploy Ollama

```bash
# Note: Requires GPU support in your cluster
kubectl apply -f ollama-deployment.yaml

# Wait for Ollama to be ready
kubectl wait --for=condition=ready pod -l app=ollama -n wolfpackai --timeout=600s

# Download the model (this takes time)
kubectl exec -it deployment/ollama -n wolfpackai -- ollama pull deepseek-coder-v2:16b
```

### 4. Deploy LiteLLM

```bash
# Create LiteLLM config ConfigMap (you'll need to create this)
# kubectl create configmap litellm-config --from-file=config.yaml=../../litellm-config.yaml -n wolfpackai

kubectl apply -f litellm-deployment.yaml

# Wait for LiteLLM to be ready
kubectl wait --for=condition=ready pod -l app=litellm -n wolfpackai --timeout=300s
```

### 5. Deploy OpenWebUI

```bash
kubectl apply -f openwebui-deployment.yaml

# Wait for OpenWebUI to be ready
kubectl wait --for=condition=ready pod -l app=openwebui -n wolfpackai --timeout=300s
```

### 6. Deploy n8n

```bash
kubectl apply -f n8n-deployment.yaml

# Wait for n8n to be ready
kubectl wait --for=condition=ready pod -l app=n8n -n wolfpackai --timeout=300s
```

### 7. Deploy Dashboard (Reverse Proxy)

```bash
# First, build and push the Dashboard image
# cd ../..
# docker build -f WolfPackAI.Dashboard/Dockerfile -t ghcr.io/YOUR_USERNAME/wolfpackai-dashboard:latest .
# docker push ghcr.io/YOUR_USERNAME/wolfpackai-dashboard:latest

kubectl apply -f dashboard-deployment.yaml
```

## Access Services

### Get LoadBalancer IP

```bash
kubectl get svc dashboard -n wolfpackai
```

### Port Forwarding (for local testing)

```bash
# Dashboard
kubectl port-forward svc/dashboard 8080:80 -n wolfpackai

# OpenWebUI (direct)
kubectl port-forward svc/openwebui 8081:8080 -n wolfpackai

# LiteLLM (direct)
kubectl port-forward svc/litellm 4000:4000 -n wolfpackai

# n8n (direct)
kubectl port-forward svc/n8n 5678:5678 -n wolfpackai
```

## Monitoring

```bash
# View all pods
kubectl get pods -n wolfpackai

# View logs
kubectl logs -f deployment/dashboard -n wolfpackai
kubectl logs -f deployment/openwebui -n wolfpackai
kubectl logs -f deployment/litellm -n wolfpackai
kubectl logs -f deployment/n8n -n wolfpackai
kubectl logs -f deployment/ollama -n wolfpackai

# Check events
kubectl get events -n wolfpackai --sort-by='.lastTimestamp'
```

## Scaling

```bash
# Scale OpenWebUI
kubectl scale deployment/openwebui --replicas=3 -n wolfpackai

# Scale LiteLLM
kubectl scale deployment/litellm --replicas=3 -n wolfpackai

# Scale Dashboard
kubectl scale deployment/dashboard --replicas=3 -n wolfpackai
```

## Cleanup

```bash
# Delete all resources
kubectl delete namespace wolfpackai

# Or delete individually
kubectl delete -f dashboard-deployment.yaml
kubectl delete -f n8n-deployment.yaml
kubectl delete -f openwebui-deployment.yaml
kubectl delete -f litellm-deployment.yaml
kubectl delete -f ollama-deployment.yaml
kubectl delete -f postgres-statefulset.yaml
kubectl delete -f secrets.yaml
kubectl delete -f namespace.yaml
```

## Production Considerations

### Security
- [ ] Update all secrets in `secrets.yaml`
- [ ] Use external secrets manager (e.g., AWS Secrets Manager, Azure Key Vault)
- [ ] Enable TLS/SSL for all services
- [ ] Implement network policies
- [ ] Use RBAC for service accounts

### High Availability
- [ ] Increase replicas for stateless services
- [ ] Set up PostgreSQL replication
- [ ] Configure anti-affinity rules
- [ ] Implement HorizontalPodAutoscaler

### Monitoring & Observability
- [ ] Install Prometheus & Grafana
- [ ] Set up log aggregation (ELK/Loki)
- [ ] Configure alerts
- [ ] Enable OpenTelemetry export

### Backup & Recovery
- [ ] Set up PostgreSQL backups
- [ ] Backup persistent volumes
- [ ] Document recovery procedures
- [ ] Test disaster recovery

### Resource Management
- [ ] Set resource requests and limits
- [ ] Configure pod disruption budgets
- [ ] Implement node affinity for GPU workloads
- [ ] Monitor resource usage

## Troubleshooting

### PostgreSQL won't start
```bash
# Check logs
kubectl logs statefulset/postgres -n wolfpackai

# Check PVC status
kubectl get pvc -n wolfpackai
```

### Ollama model download takes forever
```bash
# Check if download is progressing
kubectl logs deployment/ollama -n wolfpackai -f

# Alternative: Download model outside cluster and create PVC from volume
```

### Services can't reach each other
```bash
# Check service discovery
kubectl get svc -n wolfpackai

# Test connectivity
kubectl run test --image=busybox -it --rm -n wolfpackai -- wget -O- http://postgres:5432
```

### Dashboard returns 502
```bash
# Check upstream services are healthy
kubectl get pods -n wolfpackai

# Check dashboard logs
kubectl logs deployment/dashboard -n wolfpackai
```

## Next Steps

- Set up Ingress controller for proper domain routing
- Implement cert-manager for automatic SSL certificates
- Configure HPA for auto-scaling
- Set up external monitoring
- Implement CI/CD pipeline for automated deployments
