# LiteLLM + Claude Code Router Hybrid Routing Usage Guide

## Overview

This system combines **LiteLLM** and **Claude Code Router (CCR)** to provide intelligent AI routing that serves different use cases:

- **LiteLLM**: Handles general AI requests, chat interfaces, embeddings, vision, and speech
- **CCR**: Specializes in developer-focused Claude Code workflows with cost optimization and advanced routing

## Access Points

### Web Interfaces
- **Landing Page**: http://localhost:5000/
- **OpenWebUI (Chat)**: http://localhost:5000/chat
- **LiteLLM Admin**: http://localhost:5000/litellm/
- **Claude Code Router UI**: http://localhost:5000/ccr/ui
- **n8n Workflows**: http://localhost:5000/n8n/

### API Endpoints  
- **LiteLLM API**: http://localhost:5000/litellm/v1/
- **CCR API**: http://localhost:5000/ccr/v1/
- **Claude Code Direct**: http://localhost:5000/claude-code/v1/

### Development Environment
- **SSH Access**: `ssh developer@localhost -p 2222` (password: `devpassword`)
- **CCR Management**: Available in dev container via `ccr` command

## Usage Patterns

### 1. General AI Tasks → LiteLLM
Use LiteLLM for:
- Chat conversations through OpenWebUI
- API calls for embeddings, vision, speech
- General-purpose AI requests
- Multi-modal applications

```bash
# Chat via OpenWebUI
curl -X POST http://localhost:5000/litellm/v1/chat/completions \
  -H "Authorization: Bearer your-litellm-key" \
  -H "Content-Type: application/json" \
  -d '{"model": "claude-3-5-sonnet-20241022", "messages": [{"role": "user", "content": "Hello!"}]}'

# Embeddings
curl -X POST http://localhost:5000/litellm/v1/embeddings \
  -H "Authorization: Bearer your-litellm-key" \
  -H "Content-Type: application/json" \
  -d '{"model": "text-embedding-3-small", "input": "Your text here"}'
```

### 2. Developer Workflows → Claude Code Router
Use CCR for:
- Code generation and debugging
- Claude Code CLI interactions
- Cost-optimized model selection
- Advanced routing with `/model` overrides

```bash
# SSH into development container
ssh developer@localhost -p 2222

# Use Claude Code via CCR
ccr code "Help me debug this Python function"

# Check CCR status
ccr status

# Use specific model override
ccr code "/model deepseek,deepseek-coder Optimize this algorithm"

# Access CCR web UI for configuration
ccr ui
```

### 3. API Integration Examples

#### Direct CCR API Usage
```javascript
// Cost-optimized coding request
const response = await fetch('http://localhost:5000/ccr/v1/messages', {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer ccr-dev-key-2024',
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    messages: [
      {
        role: 'user', 
        content: 'Review this code for bugs and suggest improvements:\\n\\n```python\\n# your code here\\n```'
      }
    ],
    context: 'coder' // Routes to coding-optimized model
  })
});
```

#### LiteLLM with CCR Fallback
```javascript
// General request with CCR as fallback
const response = await fetch('http://localhost:5000/litellm/v1/chat/completions', {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer your-litellm-key',
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    model: 'ccr-claude-sonnet', // CCR-routed model
    messages: [
      {
        role: 'user',
        content: 'Generate a REST API for user management in FastAPI'
      }
    ]
  })
});
```

## Routing Decision Matrix

| Use Case | Recommended Route | Why |
|----------|-------------------|-----|
| Chat Interface | OpenWebUI → LiteLLM | User-friendly UI, conversation management |
| Code Generation | CCR Direct | Cost optimization, specialized routing |
| Embeddings/Vision | LiteLLM Direct | Broad model support, standardized APIs |
| Claude Code CLI | CCR | Native integration, advanced features |  
| Production APIs | LiteLLM | Load balancing, monitoring, caching |
| Development/Testing | CCR | Model experimentation, cost control |

## Configuration Management

### CCR Configuration
Located at `/app/code/ccr-config.json` in development container:

```bash
# SSH into container
ssh developer@localhost -p 2222

# Edit CCR configuration
nano ~/.claude-code-router/config.json

# Restart CCR with new config
ccr restart

# View CCR logs
tail -f ~/.claude-code-router/logs/ccr.log
```

### LiteLLM Configuration  
Managed in `litellm-config.yaml`:

```bash
# Edit LiteLLM config (requires app restart)
nano /path/to/litellm-config.yaml

# Add CCR integration using template
cat /app/code/litellm-ccr-config-template.yaml >> litellm-config.yaml
```

## Cost Optimization Strategies

### 1. Model Selection by Task Type

| Task Type | CCR Route | Cost Optimization |
|-----------|-----------|-------------------|
| Simple coding | `ollama-local,qwen2.5-coder` | Free (local) |
| Complex coding | `deepseek,deepseek-coder` | $0.14/$0.28 per 1M tokens |
| Reasoning | `deepseek,deepseek-reasoner` | $0.55/$2.19 per 1M tokens |
| General chat | `litellm-internal,claude-haiku` | Fallback to LiteLLM routing |

### 2. Using Model Overrides
```bash
# Force cheap model for simple tasks
ccr code "/model ollama-local,qwen2.5-coder Fix this syntax error"

# Use premium model only when needed
ccr code "/model openrouter,claude-3-5-sonnet Architect this complex system"

# Background tasks use local models
ccr code --context background "Generate unit tests"
```

### 3. Automatic Context Routing
CCR automatically routes based on:
- **Token count**: Long prompts → `longContext` models
- **Tool usage**: Function calls → `tool` specialized models  
- **Content analysis**: Code-heavy → `coder` models
- **Complexity**: Simple → local models, Complex → premium models

## Monitoring and Debugging

### Health Checks
```bash
# Check all services
curl http://localhost:5000/health

# Check CCR specifically  
curl http://localhost:3456/health

# Check LiteLLM
curl http://localhost:4000/health
```

### Logs and Monitoring
```bash
# CCR logs
tail -f ~/.claude-code-router/logs/ccr.log

# LiteLLM logs  
docker logs litellm-container

# Reverse proxy logs
docker logs reverseproxy-container

# View Aspire dashboard
# Accessible at Aspire dashboard URL
```

### Performance Metrics
- **Response Times**: Monitor via CCR web UI
- **Cost Tracking**: Built into both LiteLLM and CCR
- **Usage Analytics**: Available in respective dashboards
- **Error Rates**: Tracked with retry policies

## Troubleshooting

### Common Issues

#### CCR Not Starting
```bash
# Check CCR installation
ccr --version

# Manual start with debug
CCR_DEBUG=true ccr start

# Check port conflicts
netstat -tulpn | grep 3456
```

#### Route Not Found
- Verify reverse proxy configuration in `/ReverseProxy/appsettings.json`
- Check service URLs in clusters configuration
- Restart reverse proxy service

#### Authentication Issues
- Verify API keys in configurations
- Check CORS settings for web UI access
- Ensure proper headers in API requests

### Support Resources
- **CCR Documentation**: https://github.com/musistudio/claude-code-router
- **LiteLLM Docs**: https://docs.litellm.ai/
- **Configuration Examples**: `/app/code/` directory in dev container
- **Integration Design**: `/app/code/ccr-litellm-integration-design.md`

## Best Practices

### 1. Choose the Right Route
- Use **CCR** for development, code generation, and cost-sensitive workflows
- Use **LiteLLM** for production APIs, chat UIs, and multi-modal applications
- Use **hybrid routing** (LiteLLM with CCR models) for best of both worlds

### 2. Security Considerations
- Change default API keys in production
- Use environment variables for sensitive configuration
- Enable proper authentication and rate limiting
- Monitor usage and costs regularly

### 3. Performance Optimization
- Use local models (Ollama) for development and testing
- Cache frequently used responses
- Implement proper retry policies
- Monitor and optimize based on usage patterns

This hybrid system provides maximum flexibility while optimizing for both cost and functionality across different AI use cases.