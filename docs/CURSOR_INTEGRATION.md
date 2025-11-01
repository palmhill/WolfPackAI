# WolfPackAI Cursor Integration Guide

Complete guide for accessing WolfPackAI's AI models in Cursor IDE.

## Overview

WolfPackAI provides multiple ways to access AI models in Cursor:
- **Direct LiteLLM Access** (Primary, Working Method) - OpenAI-compatible API
- **PrimeGate API** - Model discovery and intelligent suggestions
- **Swagger UI** - Interactive API exploration

## Quick Start

### Prerequisites
1. WolfPackAI running: `dotnet run --project WolfPackAI.AppHost`
2. Cursor IDE installed
3. All services healthy (check Aspire Dashboard)

### Verify Services

Test all endpoints before configuring Cursor:

```bash
# Check PrimeGate status
curl http://localhost:7000/api/status

# Expected response:
{
  "service": "WolfPackAI.PrimeGate",
  "version": "1.0.0",
  "status": "online",
  "role": "compliant-expose-all",
  "orchestratorAuthority": "full",
  "timestamp": "2025-11-01T..."
}

# Discover available models
curl http://localhost:7000/api/llms

# Expected response:
{
  "totalModels": 4,
  "sources": {
    "ollama": 2,
    "litellm": 0,
    "cursor-native": 2
  },
  "models": [
    {
      "id": "deepseek-coder-v2:16b",
      "name": "deepseek-coder-v2:16b",
      "source": "ollama",
      "size": 8929374720,
      "capabilities": ["code", "chat", "local"],
      "parameters": { ... }
    },
    ...
  ]
}

# Check LiteLLM health
curl http://localhost:4000/health

# Check Ollama health
curl http://localhost:1143/
```

## Method 1: Direct LiteLLM Access (Recommended)

The primary working method for accessing WolfPackAI models in Cursor.

### Configuration

1. **Open Cursor Settings**
   - Press `Ctrl+,` (Windows/Linux) or `Cmd+,` (Mac)
   - Navigate to "Extensions" or "AI" settings

2. **Add Custom OpenAI-Compatible Endpoint**
   ```
   Base URL: http://localhost:4000/v1
   API Key: sk-1234 (or your configured LiteLLM master key)
   ```

3. **Available Models**
   - Any model configured in LiteLLM
   - Ollama models proxied through LiteLLM
   - Cloud models (GPT-4, Claude, Gemini) if API keys configured

### Testing in Cursor

1. **Open Cursor Chat** (`Ctrl+L`)
2. **Select Model** from dropdown
3. **Send Test Message**: "Hello, what models are available?"
4. **Verify Response** from your configured model

### Model Selection

LiteLLM exposes all configured models. To see available models:

```bash
curl http://localhost:4000/v1/models
```

Example models:
- `deepseek-coder-v2:16b` - Local Ollama model (8.9GB)
- `gpt-4-turbo` - Cloud model (requires API key)
- `claude-sonnet-4` - Cloud model (requires API key)
- `qwen3:0.6b` - Fast local model

### Advantages
- Direct OpenAI-compatible API
- No middleware overhead
- Standard Cursor integration
- Full model access
- Streaming responses

### Limitations
- Requires LiteLLM configuration
- No intelligent model suggestions
- Manual model selection

## Method 2: PrimeGate API for Model Discovery

Use PrimeGate's intelligent model suggestion system.

### PrimeGate API Endpoints

#### 1. Status Check
```bash
GET http://localhost:7000/api/status
```

Returns service health and role information.

#### 2. Model Discovery
```bash
GET http://localhost:7000/api/llms
```

Returns comprehensive inventory of all discovered models from:
- Ollama (local models)
- LiteLLM (configured cloud models)
- Cursor Native (embedded models)

**Response Structure:**
```json
{
  "totalModels": 4,
  "sources": {
    "ollama": 2,
    "litellm": 0,
    "cursor-native": 2
  },
  "models": [
    {
      "id": "deepseek-coder-v2:16b",
      "name": "deepseek-coder-v2:16b",
      "source": "ollama",
      "size": 8929374720,
      "sizeFormatted": "8.3 GB",
      "capabilities": ["code", "chat", "local"],
      "parameters": {
        "format": "gguf",
        "family": "deepseek",
        "parameter_size": "16B",
        "quantization_level": "Q4_K_M"
      }
    }
  ],
  "timestamp": "2025-11-01T..."
}
```

#### 3. Intelligent Model Suggestion
```bash
POST http://localhost:7000/api/suggest
Content-Type: application/json

{
  "description": "Refactor this complex authentication logic",
  "language": "csharp",
  "taskType": "refactor",
  "constraints": {
    "preferLocal": true,
    "maxCost": 0.01
  }
}
```

**Response:**
```json
{
  "recommended": {
    "modelId": "deepseek-coder-v2:16b",
    "source": "ollama",
    "confidence": 0.85,
    "reasoning": "Local model optimized for code refactoring tasks"
  },
  "alternatives": [
    {
      "modelId": "claude-sonnet-4",
      "source": "litellm",
      "confidence": 0.75,
      "reasoning": "Cloud model with better complex logic handling"
    }
  ]
}
```

#### 4. Task Execution
```bash
POST http://localhost:7000/api/execute
Content-Type: application/json

{
  "modelId": "deepseek-coder-v2:16b",
  "prompt": "Explain this code: async Task<User> GetUserAsync(int id)",
  "parameters": {
    "temperature": 0.7,
    "max_tokens": 1000
  }
}
```

**Response:**
```json
{
  "status": "success",
  "modelUsed": "deepseek-coder-v2:16b",
  "result": "This is an asynchronous method that...",
  "executionTimeMs": 1234,
  "tokensUsed": 156,
  "cost": 0.0
}
```

#### 5. Access Policy
```bash
GET http://localhost:7000/api/policy
```

Returns unlimited access policy for Cursor Orchestrator integration.

### Using PrimeGate in Custom Scripts

Create a simple model selector:

```python
import requests

def get_best_model(task_description, task_type="general"):
    """Query PrimeGate for the best model for a task."""
    response = requests.post(
        "http://localhost:7000/api/suggest",
        json={
            "description": task_description,
            "taskType": task_type,
            "constraints": {"preferLocal": True}
        }
    )

    if response.status_code == 200:
        data = response.json()
        model = data["recommended"]
        print(f"Best Model: {model['modelId']}")
        print(f"Confidence: {model['confidence']}")
        print(f"Reason: {model['reasoning']}")
        return model["modelId"]
    else:
        print("Error querying PrimeGate")
        return None

# Example usage
best_model = get_best_model("Refactor authentication logic", "refactor")
```

### Advantages
- Intelligent model suggestions
- Multi-source discovery (Ollama, LiteLLM, Cursor native)
- Cost optimization
- Task-specific recommendations
- Fallback strategies

### Limitations
- Requires custom integration
- Not built into Cursor UI
- Additional API layer

## Method 3: Swagger UI Exploration

Interactive API documentation for testing and development.

### Accessing Swagger

1. **Open Browser**: `http://localhost:7000/swagger`
2. **Browse Endpoints**: All 5 API endpoints documented
3. **Try It Out**: Interactive testing interface
4. **View Schemas**: Request/response models

### Swagger Features

- **Interactive Testing**: Execute API calls directly from browser
- **Request Examples**: Pre-filled sample requests
- **Response Schemas**: Detailed response structures
- **Authentication**: Test with API keys
- **Model Documentation**: Full OpenAPI specification

### Testing Workflow

1. Navigate to `http://localhost:7000/swagger`
2. Click on **GET /api/llms**
3. Click **Try it out**
4. Click **Execute**
5. View real-time response with discovered models

## Configuration Details

### PrimeGate Configuration

Location: `C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI\WolfPackAI.PrimeGate\appsettings.json`

Key settings:
```json
{
  "PrimeGate": {
    "ServiceName": "WolfPackAI.PrimeGate",
    "Version": "1.0.0",
    "Role": "compliant-expose-all",
    "Port": 7000,
    "EnableSwagger": true,
    "EnableCors": true
  },
  "Endpoints": {
    "LiteLLM": {
      "BaseUrl": "http://localhost:4000"
    },
    "Ollama": {
      "BaseUrl": "http://localhost:1143"
    }
  },
  "ModelDiscovery": {
    "EnableOllama": true,
    "EnableLiteLLM": true,
    "EnableCursorNative": true,
    "ParallelDiscovery": true
  }
}
```

### LiteLLM Configuration

Location: `C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI\WolfPackAI.AppHost\appsettings.json`

Configure models in the LiteLLM section:
```json
{
  "LiteLLM": {
    "MasterKey": "sk-1234",
    "Models": [
      {
        "model_name": "gpt-4-turbo",
        "litellm_params": {
          "model": "gpt-4-turbo",
          "api_key": "os.environ/OPENAI_API_KEY"
        }
      }
    ]
  }
}
```

## Troubleshooting

### Quick Diagnostics

```bash
# 1. Check all services
curl http://localhost:7000/api/status     # PrimeGate
curl http://localhost:4000/health          # LiteLLM
curl http://localhost:1143/                # Ollama

# 2. Discover models
curl http://localhost:7000/api/llms

# 3. Test model suggestion
curl -X POST http://localhost:7000/api/suggest \
  -H "Content-Type: application/json" \
  -d '{"description": "Write a function", "taskType": "code"}'

# 4. Test LiteLLM directly
curl http://localhost:4000/v1/models
```

### Common Issues

| Issue | Solution |
|-------|----------|
| PrimeGate returns empty models | Check Ollama and LiteLLM are running |
| Cursor can't connect | Verify LiteLLM port 4000 is accessible |
| Swagger UI not loading | Enable Swagger in appsettings.json |
| Model discovery timeout | Increase HealthCheckTimeoutSeconds |
| CORS errors | Add Cursor domain to CorsOrigins |

See [TROUBLESHOOTING_PRIMEGATE.md](./TROUBLESHOOTING_PRIMEGATE.md) for detailed solutions.

## Advanced Usage

### Custom Cursor Extension (Future)

PrimeGate is designed to support a future Cursor extension with:
- Model auto-selection based on task
- Cost optimization
- Performance monitoring
- Fallback strategies

Extension architecture:
```
Cursor IDE -> Cursor Extension -> PrimeGate API -> LiteLLM/Ollama
```

### API Integration Examples

**TypeScript/JavaScript:**
```typescript
interface TaskRequest {
  description: string;
  language?: string;
  taskType?: string;
  constraints?: {
    preferLocal?: boolean;
    maxCost?: number;
  };
}

async function suggestModel(request: TaskRequest) {
  const response = await fetch('http://localhost:7000/api/suggest', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  });

  return await response.json();
}

// Usage
const suggestion = await suggestModel({
  description: "Refactor authentication logic",
  language: "csharp",
  taskType: "refactor",
  constraints: { preferLocal: true }
});

console.log(`Use model: ${suggestion.recommended.modelId}`);
```

**C# / .NET:**
```csharp
using System.Net.Http.Json;

public class CursorIntegrationService
{
    private readonly HttpClient _httpClient;

    public CursorIntegrationService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:7000")
        };
    }

    public async Task<SuggestionResponse> SuggestModelAsync(TaskRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/suggest", request);
        return await response.Content.ReadFromJsonAsync<SuggestionResponse>();
    }
}

// Usage
var service = new CursorIntegrationService();
var suggestion = await service.SuggestModelAsync(new TaskRequest
{
    Description = "Refactor authentication logic",
    Language = "csharp",
    TaskType = "refactor"
});

Console.WriteLine($"Use model: {suggestion.Recommended.ModelId}");
```

## Next Steps

1. **Test Direct LiteLLM Integration** - Verify models work in Cursor
2. **Explore PrimeGate API** - Test model discovery and suggestions
3. **Configure Custom Models** - Add your preferred models to LiteLLM
4. **Monitor Performance** - Use Aspire Dashboard for observability
5. **Provide Feedback** - Report issues and suggest improvements

## Related Documentation

- [TROUBLESHOOTING_PRIMEGATE.md](./TROUBLESHOOTING_PRIMEGATE.md) - Detailed troubleshooting guide
- [CLAUDE.md](../CLAUDE.md) - Complete WolfPackAI architecture documentation
- [WolfPackAI.AppHost README](../WolfPackAI.AppHost/README.md) - Service orchestration details

## Support

For issues, questions, or feature requests:
1. Check the troubleshooting guide
2. Review Aspire Dashboard logs
3. Inspect PrimeGate Swagger documentation
4. Review LiteLLM configuration

---

**Last Updated:** 2025-11-01
**PrimeGate Version:** 1.0.0
**Status:** Production Ready
