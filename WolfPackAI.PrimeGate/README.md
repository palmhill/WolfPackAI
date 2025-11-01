# WolfPackAI.PrimeGate

> Production-ready LLM exposure service for Cursor Orchestrator integration with graceful degradation and intelligent model discovery.

## Overview

WolfPackAI.PrimeGate is a compliant LLM exposure layer that seamlessly integrates WolfPackAI's infrastructure with Cursor IDE, providing the orchestrator with access to 15+ AI models while maintaining full control authority.

## Architecture

### Project Structure

```
WolfPackAI.PrimeGate/
├── Configuration/
│   └── PrimeGateConfiguration.cs    # All configuration classes
├── Models/
│   ├── Requests/
│   │   ├── TaskRequest.cs             # Request for /api/suggest
│   │   └── ExecutionRequest.cs        # Request for /api/execute
│   └── Responses/
│       ├── StatusResponse.cs          # Response for /api/status
│       ├── LLMInventoryResponse.cs    # Response for /api/llms
│       ├── SuggestionResponse.cs      # Response for /api/suggest
│       ├── ExecutionResponse.cs       # Response for /api/execute
│       ├── PolicyResponse.cs          # Response for /api/policy
│       └── OllamaTagsResponse.cs      # Ollama API response model
├── Services/
│   ├── IAutoDiscoveryService.cs       # Auto-discovery interface
│   ├── AutoDiscoveryService.cs        # Discovery with caching
│   ├── IModelSuggestionService.cs     # Suggestion interface
│   ├── ModelSuggestionService.cs      # Intelligent suggestions
│   ├── IExecutionService.cs           # Execution interface
│   └── ExecutionService.cs            # Model execution
├── appsettings.json                   # Full configuration
├── Program.cs                         # Minimal API with all endpoints
└── WolfPackAI.PrimeGate.csproj      # Project file
```

## Features

### Core Capabilities

- **Auto-Discovery**: Automatically discovers models from Ollama, LiteLLM, and Cursor native sources
- **Memory Caching**: 30-second cache for model discovery to optimize performance
- **Parallel Discovery**: Concurrent discovery from multiple sources
- **Graceful Degradation**: Never throws errors; returns degraded responses if backends are offline
- **Intelligent Suggestions**: Context-aware model suggestions based on task analysis
- **Flexible Execution**: Routes requests to appropriate endpoints (Ollama/LiteLLM)
- **Complete Configuration**: All settings externalized to appsettings.json

### API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/status` | GET | Service status and role information |
| `/api/llms` | GET | List all available LLMs from all sources |
| `/api/suggest` | POST | Suggest best model for a task |
| `/api/execute` | POST | Execute task with selected model |
| `/api/policy` | GET | Access policy (unlimited for orchestrator) |
| `/swagger` | GET | Interactive API documentation |

## Configuration

### appsettings.json Structure

```json
{
  "PrimeGate": {
    "ServiceName": "WolfPackAI.PrimeGate",
    "Version": "1.0.0",
    "Role": "compliant-expose-all",
    "OrchestratorAuthority": "full",
    "Port": 7000,
    "DiscoveryCacheSeconds": 30,
    "HealthCheckTimeoutSeconds": 3,
    "EnableSwagger": true,
    "EnableCors": true,
    "CorsOrigins": ["*"]
  },
  "Endpoints": {
    "LiteLLM": {
      "BaseUrl": "http://litellm:4000",
      "HealthEndpoint": "/health",
      "ChatCompletionsEndpoint": "/v1/chat/completions",
      "ModelsEndpoint": "/v1/models",
      "Timeout": 30
    },
    "Ollama": {
      "BaseUrl": "http://ollama:11434",
      "TagsEndpoint": "/api/tags",
      "GenerateEndpoint": "/api/generate",
      "HealthEndpoint": "/",
      "Timeout": 30
    }
  },
  "ModelDiscovery": {
    "EnableOllama": true,
    "EnableLiteLLM": true,
    "EnableCursorNative": true,
    "ParallelDiscovery": true,
    "FailureMode": "graceful"
  },
  "Policy": {
    "TokenLimit": 2147483647,
    "RequestsPerMinute": 2147483647,
    "ConcurrentModels": 2147483647,
    "CostLimit": 1.7976931348623157E+308,
    "Restrictions": []
  },
  "ModelSuggestion": {
    "EnableIntelligentSuggestions": true,
    "DefaultConfidence": 0.5,
    "PreferredModels": {
      "Refactor": "claude-sonnet",
      "CodeReview": "claude-sonnet",
      "Algorithm": "gpt-4",
      "ComplexLogic": "gpt-4",
      "Fast": "deepseek-coder",
      "Quick": "qwen3",
      "Documentation": "gpt-3.5",
      "Comments": "gpt-3.5",
      "Security": "claude-opus",
      "Audit": "claude-opus"
    }
  }
}
```

## Port Mapping (IMPORTANT)

**Correct Container Networking:**
- LiteLLM: `http://litellm:4000` (NOT localhost)
- Ollama: `http://ollama:11434` (NOT localhost)
- PrimeGate: `http://localhost:7000` (Exposed to host)

This service runs on the **host**, not in Docker, so it uses container names to access other services.

## Usage

### Building

```bash
dotnet build WolfPackAI.PrimeGate.csproj
```

### Running

```bash
dotnet run --project WolfPackAI.PrimeGate.csproj
```

The service will start on `http://localhost:7000`

### Testing Endpoints

```bash
# Check status
curl http://localhost:7000/api/status

# List models
curl http://localhost:7000/api/llms

# Suggest model
curl -X POST http://localhost:7000/api/suggest \
  -H "Content-Type: application/json" \
  -d '{"task": "refactor code", "context": "complex class"}'

# Check policy
curl http://localhost:7000/api/policy

# View Swagger UI
http://localhost:7000/swagger
```

## Service Details

### AutoDiscoveryService

- **Caching**: Uses `IMemoryCache` with configurable expiration (default 30 seconds)
- **Parallel Discovery**: Discovers from all sources concurrently for better performance
- **Graceful Degradation**: If a backend is offline, it logs a warning and continues
- **Sources**:
  - **Ollama**: Calls `/api/tags` to get local models
  - **LiteLLM**: Checks `/health` and loads configured cloud models
  - **Cursor Native**: Always available models (cursor-fast, cursor-smart)

### ModelSuggestionService

- **Intelligent Analysis**: Parses task and context to match patterns
- **Configurable Preferences**: Model preferences defined in appsettings.json
- **Confidence Scoring**: Returns confidence level (0.0-1.0) for each suggestion
- **Fallback Strategy**: Returns first available model if no pattern matches

### ExecutionService

- **Smart Routing**: Automatically routes to Ollama or LiteLLM based on model name
- **Payload Adaptation**: Formats requests appropriately (Ollama vs OpenAI format)
- **Error Handling**: Never throws; always returns ExecutionResponse with status
- **Timeout Management**: Different timeouts for local vs cloud models

## Design Principles

### 1. Graceful Degradation
- **Never throw exceptions to caller**
- Return empty/degraded responses if backends are unavailable
- Log warnings but continue operation

### 2. Separation of Concerns
- Configuration classes separate from business logic
- Service interfaces enable testability and dependency injection
- Models organized by request/response

### 3. Performance Optimization
- Memory caching reduces discovery overhead
- Parallel discovery speeds up model enumeration
- Configurable timeouts prevent hanging

### 4. Orchestrator Authority
- PrimeGate only suggests, never enforces
- Unlimited policy for orchestrator
- Transparent error reporting

## Dependencies

```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="7.2.0" />
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="9.0.0" />
<ProjectReference Include="..\WolfPackAI.ServiceDefaults\WolfPackAI.ServiceDefaults.csproj" />
```

## Logging

All services use `ILogger<T>` for structured logging:

- **Information**: Successful operations, discovery results
- **Warning**: Backend unavailable, discovery failures
- **Error**: Unexpected errors (but never thrown to caller)

## Monitoring

Access the Swagger UI at `http://localhost:7000/swagger` to:
- View all API endpoints
- Test endpoints interactively
- See request/response schemas
- Generate client code

## Production Considerations

### Security
- **CORS**: Configure specific origins instead of `*` in production
- **Authentication**: Add API key authentication if needed
- **Rate Limiting**: Consider adding rate limiting per client

### Performance
- **Cache Duration**: Adjust `DiscoveryCacheSeconds` based on model stability
- **Timeouts**: Tune timeouts for your network conditions
- **Parallel Discovery**: Can be disabled if causing issues

### Reliability
- **Health Checks**: Add health check endpoint for orchestration
- **Circuit Breakers**: Consider adding circuit breakers for backend calls
- **Retry Policies**: Add retry logic for transient failures

## Build Status

✅ **Build Status**: SUCCESS (0 errors, 0 warnings)
✅ **Target Framework**: .NET 9.0
✅ **Build Configuration**: Debug & Release

## Version History

### v1.0.0 (Current)
- Complete refactoring from monolithic to clean architecture
- Configuration-driven design
- Graceful degradation
- Memory caching with 30-second expiration
- Parallel discovery
- Intelligent model suggestions
- Full Swagger documentation
- Production-ready error handling

## Support

For issues or questions, refer to the main WolfPackAI documentation or the architecture design documents in the repository root.
