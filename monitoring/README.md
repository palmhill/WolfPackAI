# WolfPackAI Monitoring Stack

Comprehensive observability for WolfPackAI using Prometheus, Grafana, and OpenTelemetry.

## Components

### Prometheus
- **Metrics collection** from all services
- **Time-series database** for metric storage
- **Alerting engine** for notifications
- **Web UI**: http://localhost:9090

### Grafana
- **Dashboard visualization** for metrics
- **Pre-configured dashboards** for WolfPackAI
- **Alerting and notifications**
- **Web UI**: http://localhost:3000
- **Default credentials**: admin/admin

### OpenTelemetry
- **Distributed tracing** across services
- **Metrics export** to Prometheus
- **Logs correlation**
- Integrated via Aspire Service Defaults

## Quick Start

### Option 1: Add to Aspire AppHost

Edit `WolfPackAI.AppHost/Program.cs`:

```csharp
// Add monitoring services
var prometheus = builder.AddPrometheus();
var grafana = builder.AddGrafana(prometheus);
```

### Option 2: Standalone Docker Compose

```bash
cd monitoring
docker-compose -f docker-compose.monitoring.yml up -d
```

## Accessing Dashboards

### Grafana
1. Open http://localhost:3000
2. Login with `admin` / `admin`
3. Navigate to **Dashboards** → **WolfPackAI Overview**

### Prometheus
1. Open http://localhost:9090
2. Query metrics directly
3. View targets at http://localhost:9090/targets

## Available Metrics

### Service Health
- `up{job="<service>"}` - Service availability (1 = up, 0 = down)

### LiteLLM Metrics
- `litellm_total_requests` - Total API requests
- `litellm_total_tokens` - Total tokens consumed
- `litellm_request_duration_seconds` - Request latency
- `litellm_model_cost_usd` - Cost per model
- `litellm_error_rate` - Error percentage

### OpenWebUI Metrics
- `openwebui_active_users` - Current active users
- `openwebui_total_messages` - Total chat messages
- `openwebui_session_duration_seconds` - User session length

### System Metrics (via OpenTelemetry)
- `process_cpu_usage` - CPU utilization
- `process_memory_usage_bytes` - Memory consumption
- `http_server_requests_total` - HTTP request count
- `http_server_request_duration_seconds` - HTTP latency

### Database Metrics (PostgreSQL Exporter)
- `pg_up` - Database availability
- `pg_stat_database_numbackends` - Active connections
- `pg_stat_database_xact_commit` - Transaction rate

## Pre-configured Dashboards

### 1. WolfPackAI Overview
- Service health status
- Request rates
- Active users
- Token usage
- Response latency

### 2. AI Model Performance
- Tokens per second
- Cost per model
- Request success rate
- Model comparison

### 3. System Resources
- CPU usage per service
- Memory consumption
- Disk I/O
- Network traffic

### 4. Database Performance
- Connection pool usage
- Query duration
- Transaction rate
- Cache hit ratio

## Alerting

### Configure Alert Rules

Edit `prometheus.yml`:

```yaml
rule_files:
  - 'alerts.yml'

alerting:
  alertmanagers:
    - static_configs:
      - targets: ['alertmanager:9093']
```

### Example Alerts (`alerts.yml`)

```yaml
groups:
  - name: wolfpackai_alerts
    interval: 30s
    rules:
      - alert: ServiceDown
        expr: up == 0
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "Service {{$labels.job}} is down"

      - alert: HighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.05
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High error rate on {{$labels.job}}"

      - alert: HighLatency
        expr: histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 2
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "High latency on {{$labels.job}}"
```

## Custom Metrics

### Add Metrics to Your Code

```csharp
using System.Diagnostics.Metrics;

// Create meter
var meter = new Meter("WolfPackAI.Custom");

// Counter
var requestCounter = meter.CreateCounter<long>("custom_requests_total");
requestCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "/api/custom"));

// Histogram
var duration = meter.CreateHistogram<double>("custom_duration_seconds");
duration.Record(0.5, new KeyValuePair<string, object?>("operation", "process"));
```

### Metrics are automatically exported via OpenTelemetry!

## Querying Metrics

### Prometheus Query Examples

```promql
# CPU usage by service
rate(process_cpu_seconds_total[5m])

# Memory usage in MB
process_resident_memory_bytes / 1024 / 1024

# Request rate per service
rate(http_server_requests_total[5m])

# P95 latency
histogram_quantile(0.95, rate(http_server_request_duration_seconds_bucket[5m]))

# Error percentage
(sum(rate(http_server_requests_total{status=~"5.."}[5m])) /
 sum(rate(http_server_requests_total[5m]))) * 100
```

## Troubleshooting

### Metrics Not Appearing

**Check**:
1. Prometheus targets: http://localhost:9090/targets
2. Service exposes `/metrics` endpoint
3. Firewall allows connections to metric ports
4. OpenTelemetry exporter configured

### Grafana Can't Connect to Prometheus

**Fix**:
1. Verify Prometheus is running: `docker ps | grep prometheus`
2. Check datasource URL in Grafana settings
3. Test connectivity: `curl http://prometheus:9090/api/v1/targets`

### High Memory Usage

**Solutions**:
1. Reduce retention period in `prometheus.yml`
2. Increase scrape intervals
3. Add resource limits in Docker Compose

## Production Best Practices

### Security
- [ ] Change Grafana default password
- [ ] Enable Prometheus authentication
- [ ] Use TLS for all connections
- [ ] Restrict network access

### Scaling
- [ ] Use Prometheus federation for multiple clusters
- [ ] Enable Grafana high availability
- [ ] Set up long-term storage (Thanos/Cortex)

### Reliability
- [ ] Configure alert notifications (Slack/PagerDuty)
- [ ] Set up monitoring for monitoring services
- [ ] Automate backup of Grafana dashboards
- [ ] Test disaster recovery procedures

## Resources

- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Aspire Observability](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview)
