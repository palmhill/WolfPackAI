# CLAUDE.md

This file provides comprehensive guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Development Commands

### Build and Run
- **Build the solution**: `dotnet build`
- **Run the main application**: `dotnet run --project WolfPackAI.AppHost`
- **Run specific project**: `dotnet run --project <ProjectName>`
- **Clean build artifacts**: `dotnet clean`

### Testing
- **Run all tests**: `dotnet test`
- **Run tests with verbose output**: `dotnet test --verbosity normal`

## Architecture Overview

This is a sophisticated .NET Aspire-orchestrated solution that integrates multiple AI services including OpenWebUI, LiteLLM, Claude Code Router (CCR), and n8n workflow automation. The architecture provides a comprehensive AI development and deployment platform.

### Core Projects
1. **WolfPackAI.AppHost** - The main Aspire orchestration host that configures and manages all containerized services
2. **WolfPackAI.ReverseProxy** - YARP-based reverse proxy that routes requests between services with advanced path rewriting
3. **WolfPackAI.ServiceDefaults** - Shared service configuration and extensions for OpenTelemetry, health checks, and resilience

### Service Architecture

The application orchestrates multiple Docker containers in a microservices architecture:

- **OpenWebUI** (`ghcr.io/open-webui/open-webui:latest`) - AI chat interface on port 8080
- **LiteLLM** (`ghcr.io/berriai/litellm-database:main-v1.74.8-nightly`) - LLM proxy/gateway on port 4000
- **Ollama** (`ollama/ollama:latest`) - Local LLM runtime (qwen3:0.6b model) on port 1143 with GPU support
- **PostgreSQL** - Primary database with multiple schemas (openwebuidb, litellmdb, n8ndb)
- **Claude Code Router (CCR)** - Developer-focused AI routing service on port 3456
- **n8n** (`docker.n8n.io/n8nio/n8n`) - Workflow automation platform on port 5678
- **Development Container** - Multi-language development environment with SSH access on port 2222
- **Reverse Proxy** - YARP-based traffic routing on ports 80/443

### Configuration System

All configuration is centralized in `WolfPackAI.AppHost/appsettings.json` with strongly-typed configuration classes:

#### Configuration Sections:
- **LiteLLM**: Model configurations, routing settings, and master key
- **Auth**: Azure AD integration settings (tenant, client ID/secret)
- **OpenWebUI**: Public URL configuration for OAuth callbacks
- **Postgres**: Database connection settings and credentials
- **PublicNetwork**: External networking configuration (HTTP/HTTPS ports, public URL)

The system features dynamic YAML generation that converts JSON configuration to LiteLLM-compatible YAML format using custom serialization logic in `Configuration.cs`.

### Key Components

#### Program.cs (AppHost) - `WolfPackAI.AppHost/Program.cs`
- **Configuration Loading & Validation**: Validates all service configurations at startup
- **Service Dependency Management**: Sets up proper startup ordering with health checks
- **Container Orchestration**: Configures all Docker containers with environment variables, volumes, and networking
- **Dynamic YAML Generation**: Creates LiteLLM configuration file from appsettings.json
- **Multi-Service Integration**: Manages dependencies between OpenWebUI, LiteLLM, Ollama, PostgreSQL, n8n, and CCR

#### Configuration.cs - `WolfPackAI.AppHost/Configuration.cs`
- **Strongly-Typed Models**: Configuration classes with JSON and YAML serialization attributes
- **Validation Logic**: Ensures all required configuration values are present with detailed error messages
- **YAML Serialization**: Converts C# configuration objects to LiteLLM-compatible YAML format using YamlDotNet
- **Environment Variable Integration**: Handles secure API key references via environment variables

#### WolfPackAI.ReverseProxy/Program.cs - `WolfPackAI.ReverseProxy/Program.cs`
- **YARP Configuration**: Advanced request routing with path transformation and header management
- **Landing Page**: Serves a responsive HTML portal at `/` with links to all services
- **Response Transformation**: Handles Location header rewriting for proper service routing
- **Service Path Management**: Routes to /chat, /litellm/, /ccr/, /n8n/, and /claude-code/ endpoints
- **Static File Serving**: Serves static assets from wwwroot directory

#### Ollama Integration - `WolfPackAI.AppHost/OllamaResource.cs`
- **Custom Resource Definition**: Extends ContainerResource for Ollama-specific functionality
- **Connection String Generation**: Provides connection strings for other services
- **GPU Support**: Configurable GPU acceleration for model inference
- **Model Management**: Handles model downloading and lifecycle management

#### Service Extensions - `WolfPackAI.AppHost/ResourceExtensions.cs`
- **Ollama Resource Builder**: Extension methods for configuring Ollama containers
- **GPU Configuration**: Support for GPU acceleration with runtime arguments
- **Port Management**: Flexible host port configuration
- **Volume Management**: Persistent storage for Ollama models

### Authentication & Security

The system uses Azure Active Directory for centralized authentication:

#### OAuth/OIDC Flow:
1. **OpenWebUI Integration**: Configured with OAuth OIDC provider pointing to Azure AD
2. **Dynamic Callback URLs**: OAuth callback URL automatically constructed based on public URL configuration
3. **Email-Based Account Merging**: User authentication handled entirely through Azure AD with account consolidation
4. **Service-to-Service Authentication**: LiteLLM master key used for internal service communication

#### Security Features:
- **Environment-Based Secrets**: API keys referenced via environment variables
- **CORS Configuration**: Configurable cross-origin resource sharing policies
- **User Secrets Integration**: Support for .NET user secrets in development
- **Basic Authentication**: n8n protected with configurable credentials

### Data Persistence & Health Monitoring

#### Database Architecture:
- **PostgreSQL Primary Database**: Serves as the backbone for all services
- **Multi-Tenant Schema Design**: Separate databases for each service (openwebuidb, litellmdb, n8ndb)
- **Persistent Volume Management**: Docker volumes maintain data persistence across container restarts
- **Connection Pooling**: Optimized database connections with proper resource management

#### Health Check System:
- **Service Health Monitoring**: Each service configured with appropriate health check endpoints
- **Dependency Management**: Services wait for dependencies before startup
- **OpenTelemetry Integration**: Comprehensive observability with metrics, traces, and logs
- **Aspire Dashboard**: Real-time monitoring of all service health and performance

### Development Environment

#### Multi-Language Development Container:
- **SSH Access**: Full SSH server with developer user account
- **Language Support**: Pre-configured with .NET 9.0, Node.js 22.x, Python 3, C++, and development tools
- **Claude Code Router**: Integrated CCR installation with configuration
- **Development Tools**: Git, Vim, Nano, build-essential, and modern development utilities
- **Port Forwarding**: SSH on 2222, CCR on 3456

#### Service Access Points:
- **Landing Page**: `http://localhost:5000/` - Service portal with navigation
- **OpenWebUI Chat**: `http://localhost:5000/chat` - AI chat interface
- **LiteLLM Admin**: `http://localhost:5000/litellm/` - LLM gateway management
- **Claude Code Router UI**: `http://localhost:5000/ccr/ui` - CCR configuration interface
- **n8n Workflows**: `http://localhost:5000/n8n/` - Workflow automation platform
- **SSH Development**: `ssh developer@localhost -p 2222` - Development environment access

### Claude Code Router Integration

#### Hybrid AI Routing System:
The system implements a sophisticated routing strategy that leverages both LiteLLM and Claude Code Router:

- **LiteLLM**: Handles general AI requests, chat interfaces, embeddings, vision, and speech processing
- **CCR**: Specializes in developer-focused Claude Code workflows with cost optimization and advanced routing
- **Fallback Strategy**: CCR configured to fall back to LiteLLM for broad model compatibility
- **Cost Optimization**: Intelligent routing based on model costs and request patterns

#### API Endpoints:
- **LiteLLM API**: `http://localhost:5000/litellm/v1/` - OpenAI-compatible API
- **CCR API**: `http://localhost:5000/ccr/v1/` - Claude Code Router API
- **Direct Claude**: `http://localhost:5000/claude-code/v1/` - Direct Claude Code access

### Workflow Automation with n8n

#### Features:
- **Database Integration**: PostgreSQL backend for workflow persistence
- **Webhook Support**: External webhook integration with proper URL configuration
- **Basic Authentication**: Secured with configurable credentials
- **Service Integration**: Direct access to all other platform services
- **Timezone Configuration**: Configurable timezone support (default: Europe/London)

### PrimeGate - Cursor IDE Integration Service

#### Purpose and Features
WolfPackAI.PrimeGate is a compliant LLM exposure service designed for seamless integration with Cursor IDE and other development tools. It provides intelligent model discovery, suggestion, and execution capabilities while maintaining full orchestrator authority.

**Core Features:**
- **Multi-Source Model Discovery**: Automatically discovers models from Ollama, LiteLLM, and Cursor native sources
- **Intelligent Model Suggestions**: Task-specific model recommendations with confidence scoring
- **Cost Optimization**: Suggests models based on cost constraints and performance requirements
- **Compliant Architecture**: Full orchestrator authority with unlimited access policy
- **OpenAPI Documentation**: Interactive Swagger UI for API exploration and testing
- **CORS Support**: Configurable cross-origin resource sharing for web-based integrations

#### Technology Stack

**Framework & Runtime:**
- **.NET 9.0**: Modern web API built on ASP.NET Core
- **Minimal APIs**: Lightweight, high-performance endpoint definitions
- **Dependency Injection**: Built-in service container for extensibility

**Key Dependencies:**
- **Swashbuckle.AspNetCore 7.2.0**: OpenAPI/Swagger documentation generation
- **Microsoft.Extensions.Caching.Memory**: In-memory caching for model discovery results
- **WolfPackAI.ServiceDefaults**: Shared observability and health check configuration

**Architecture Patterns:**
- **Auto-Discovery Service**: Parallel health checks and model inventory from multiple sources
- **Model Suggestion Engine**: Configurable intelligent recommendations based on task characteristics
- **Execution Service**: Direct task execution with fallback strategies
- **Configuration-Driven**: Strongly-typed settings from appsettings.json

#### API Endpoints

PrimeGate exposes 5 RESTful API endpoints:

**1. GET /api/status** - Service Health and Information
```json
{
  "service": "WolfPackAI.PrimeGate",
  "version": "1.0.0",
  "status": "online",
  "role": "compliant-expose-all",
  "orchestratorAuthority": "full",
  "timestamp": "2025-11-01T..."
}
```

**2. GET /api/llms** - Comprehensive Model Discovery
Returns inventory of all discovered models from:
- Ollama local models (with size, parameters, capabilities)
- LiteLLM configured cloud models (GPT-4, Claude, Gemini, etc.)
- Cursor native embedded models (cursor-fast, cursor-smart)

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
        "parameter_size": "16B"
      }
    }
  ]
}
```

**3. POST /api/suggest** - Intelligent Model Suggestion
Analyzes task requirements and suggests optimal models with reasoning:
```json
// Request
{
  "description": "Refactor complex authentication logic",
  "language": "csharp",
  "taskType": "refactor",
  "constraints": {
    "preferLocal": true,
    "maxCost": 0.01
  }
}

// Response
{
  "recommended": {
    "modelId": "deepseek-coder-v2:16b",
    "source": "ollama",
    "confidence": 0.85,
    "reasoning": "Local model optimized for code refactoring"
  },
  "alternatives": [...]
}
```

**4. POST /api/execute** - Task Execution
Executes tasks with selected models and returns results:
```json
// Request
{
  "modelId": "deepseek-coder-v2:16b",
  "prompt": "Explain this async method",
  "parameters": {
    "temperature": 0.7,
    "max_tokens": 1000
  }
}

// Response
{
  "status": "success",
  "modelUsed": "deepseek-coder-v2:16b",
  "result": "This is an asynchronous method that...",
  "executionTimeMs": 1234,
  "tokensUsed": 156,
  "cost": 0.0
}
```

**5. GET /api/policy** - Access Policy Information
Returns unlimited access policy for orchestrator integration:
```json
{
  "tokenLimit": 2147483647,
  "requestsPerMinute": 2147483647,
  "concurrentModels": 2147483647,
  "costLimit": 1.7976931348623157E+308,
  "restrictions": [],
  "note": "Orchestrator has unlimited access to all resources"
}
```

#### Configuration Details

**Location**: `WolfPackAI.PrimeGate/appsettings.json`

**PrimeGate Settings:**
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
  }
}
```

**Endpoint Configuration:**
```json
{
  "Endpoints": {
    "LiteLLM": {
      "BaseUrl": "http://localhost:4000",
      "HealthEndpoint": "/health",
      "ChatCompletionsEndpoint": "/v1/chat/completions",
      "ModelsEndpoint": "/v1/models",
      "Timeout": 30
    },
    "Ollama": {
      "BaseUrl": "http://localhost:1143",
      "TagsEndpoint": "/api/tags",
      "GenerateEndpoint": "/api/generate",
      "HealthEndpoint": "/",
      "Timeout": 30
    }
  }
}
```

**Model Discovery Configuration:**
```json
{
  "ModelDiscovery": {
    "EnableOllama": true,
    "EnableLiteLLM": true,
    "EnableCursorNative": true,
    "ParallelDiscovery": true,
    "FailureMode": "graceful"
  }
}
```

**Model Suggestion Preferences:**
```json
{
  "ModelSuggestion": {
    "EnableIntelligentSuggestions": true,
    "PreferredModels": {
      "Refactor": "claude-sonnet",
      "CodeReview": "claude-sonnet",
      "Algorithm": "gpt-4",
      "Fast": "deepseek-coder",
      "Security": "claude-opus"
    }
  }
}
```

#### Cursor Integration Methods

**Method 1: Direct LiteLLM Access (Primary)**
Configure Cursor to use LiteLLM's OpenAI-compatible endpoint:
- Base URL: `http://localhost:4000/v1`
- API Key: LiteLLM master key from configuration
- Models: All LiteLLM-configured models available in Cursor dropdown

**Method 2: PrimeGate API for Model Discovery**
Use PrimeGate's intelligent suggestion system:
- Query `/api/llms` to discover all available models
- Use `/api/suggest` for task-specific model recommendations
- Execute tasks via `/api/execute` with automatic routing

**Method 3: Swagger UI Exploration**
Interactive API testing and documentation:
- URL: `http://localhost:7000/swagger`
- Test all endpoints directly from browser
- View request/response schemas
- Generate client code examples

See [docs/CURSOR_INTEGRATION.md](docs/CURSOR_INTEGRATION.md) for complete integration guide.

#### Service Access Points

- **PrimeGate API**: `http://localhost:7000/api/` - Main API endpoints
- **Swagger UI**: `http://localhost:7000/swagger` - Interactive API documentation
- **Health Check**: `http://localhost:7000/api/status` - Service health monitoring
- **Model Discovery**: `http://localhost:7000/api/llms` - Real-time model inventory

#### Architecture Overview

**Service Responsibilities:**
1. **Auto-Discovery**: Parallel health checks and model enumeration from Ollama, LiteLLM, and Cursor native sources
2. **Intelligent Routing**: Task analysis and optimal model selection based on capabilities and constraints
3. **Execution Proxy**: Unified execution interface with fallback strategies
4. **Policy Enforcement**: Access control and resource management (currently unlimited for orchestrator)
5. **Observability**: OpenTelemetry integration for metrics, traces, and logging

**Data Flow:**
```
Cursor IDE / Client
    ↓
PrimeGate API (Port 7000)
    ↓
Model Discovery Service
    ↓
┌─────────────┬──────────────┬────────────────┐
│   Ollama    │   LiteLLM    │  Cursor Native │
│  (1143)     │   (4000)     │   (embedded)   │
└─────────────┴──────────────┴────────────────┘
```

**Caching Strategy:**
- Discovery results cached for 30 seconds (configurable)
- Cache invalidation on health check failures
- Per-source caching with parallel refresh

**Error Handling:**
- Graceful degradation on source failures
- Alternative model suggestions on execution errors
- Detailed error responses with troubleshooting hints

#### Development Workflow

**Running PrimeGate Standalone:**
```bash
# Build the project
dotnet build WolfPackAI.PrimeGate

# Run the service
dotnet run --project WolfPackAI.PrimeGate

# Verify service is running
curl http://localhost:7000/api/status
```

**Testing API Endpoints:**
```bash
# 1. Check service health
curl http://localhost:7000/api/status

# 2. Discover all models
curl http://localhost:7000/api/llms

# 3. Get model suggestion
curl -X POST http://localhost:7000/api/suggest \
  -H "Content-Type: application/json" \
  -d '{"description": "Refactor code", "taskType": "refactor"}'

# 4. Execute a task
curl -X POST http://localhost:7000/api/execute \
  -H "Content-Type: application/json" \
  -d '{"modelId": "deepseek-coder-v2:16b", "prompt": "Hello"}'

# 5. Check access policy
curl http://localhost:7000/api/policy
```

**Integration with Aspire:**
PrimeGate is designed to be orchestrated by WolfPackAI.AppHost:
- Automatic service discovery
- Health check integration
- OpenTelemetry metrics collection
- Dependency management with LiteLLM and Ollama

**Development Tools:**
- **Swagger UI**: Interactive API testing at `http://localhost:7000/swagger`
- **Health Checks**: Built-in ASP.NET Core health endpoints
- **Structured Logging**: Console and OpenTelemetry logging
- **Hot Reload**: .NET 9.0 hot reload for rapid development

#### File Locations

**Core Files:**
- `WolfPackAI.PrimeGate/Program.cs` - Main application entry point and API endpoint definitions
- `WolfPackAI.PrimeGate/appsettings.json` - Complete service configuration
- `WolfPackAI.PrimeGate/WolfPackAI.PrimeGate.csproj` - Project file with dependencies

**Configuration Models:**
- `Configuration/PrimeGateSettings.cs` - Service settings
- `Configuration/EndpointsConfiguration.cs` - LiteLLM and Ollama endpoints
- `Configuration/ModelDiscoveryConfiguration.cs` - Discovery behavior
- `Configuration/PolicyConfiguration.cs` - Access policy settings
- `Configuration/ModelSuggestionConfiguration.cs` - Suggestion preferences

**Services:**
- `Services/IAutoDiscoveryService.cs` - Model discovery interface
- `Services/AutoDiscoveryService.cs` - Multi-source model discovery implementation
- `Services/IModelSuggestionService.cs` - Model suggestion interface
- `Services/ModelSuggestionService.cs` - Intelligent suggestion engine
- `Services/IExecutionService.cs` - Task execution interface
- `Services/ExecutionService.cs` - Unified execution with fallbacks

**Models:**
- `Models/Requests/TaskRequest.cs` - Task suggestion request
- `Models/Requests/ExecutionRequest.cs` - Task execution request
- `Models/Responses/StatusResponse.cs` - Service status
- `Models/Responses/LLMInventoryResponse.cs` - Model discovery results
- `Models/Responses/SuggestionResponse.cs` - Model suggestions
- `Models/Responses/ExecutionResponse.cs` - Execution results
- `Models/Responses/PolicyResponse.cs` - Access policy

**Documentation:**
- `docs/CURSOR_INTEGRATION.md` - Complete Cursor integration guide
- `docs/TROUBLESHOOTING_PRIMEGATE.md` - Troubleshooting and diagnostics

### Development Workflow

#### Getting Started:
1. **Configure `appsettings.json`** with your Azure AD tenant and API settings
2. **Run the application**: `dotnet run --project WolfPackAI.AppHost`
3. **Access Aspire Dashboard** to monitor service health and logs
4. **Navigate to landing page** at `http://localhost:5000/` for service access
5. **Use SSH for development**: `ssh developer@localhost -p 2222` (password: `devpassword`)

#### Service Dependencies & Startup Order:
1. **PostgreSQL** starts first as the foundational data layer
2. **Ollama** initializes and downloads required models
3. **LiteLLM** starts after PostgreSQL and Ollama are healthy
4. **OpenWebUI** waits for both PostgreSQL and LiteLLM
5. **n8n** starts after PostgreSQL is available
6. **Development Container** initializes with CCR configuration
7. **Reverse Proxy** starts last, waiting for all upstream services

### Service Configuration Details

#### OpenWebUI Environment Variables:
- `ENABLE_PERSISTENT_CONFIG`: false (configuration managed by Aspire)
- `WEBUI_URL`: Dynamic based on PublicUrl configuration
- `ENABLE_OAUTH_SIGNUP`: true for Azure AD integration
- `OAUTH_MERGE_ACCOUNTS_BY_EMAIL`: true for seamless user experience
- `OPENAI_API_BASE_URL`: Points to LiteLLM service endpoint
- `DATABASE_URL`: PostgreSQL connection for OpenWebUI data
- `CORS_ALLOW_ORIGIN`: Configurable for security (default: *)

#### LiteLLM Environment Variables:
- `STORE_MODEL_IN_DB`: true for persistent model configuration
- `LITELLM_MASTER_KEY`: Service-to-service authentication key
- `SERVER_ROOT_PATH`: `/litellm` for reverse proxy compatibility
- `DATABASE_URL`: PostgreSQL connection for LiteLLM data
- `UI_USERNAME`/`UI_PASSWORD`: Admin interface credentials

#### Development Container Setup:
- **System Packages**: Ubuntu 24.04 with comprehensive development tools
- **SSH Configuration**: Secure remote access with password and key authentication
- **CCR Installation**: Latest Claude Code Router with configuration
- **Multi-Language Support**: .NET, Node.js, Python, C++ toolchains
- **Network Integration**: Full access to all container services

### Monitoring & Observability

#### OpenTelemetry Integration:
- **Metrics Collection**: ASP.NET Core, HTTP client, and runtime metrics
- **Distributed Tracing**: Request tracing across all services
- **Structured Logging**: Comprehensive logging with OpenTelemetry formatting
- **OTLP Export**: Support for external observability platforms

#### Health Check Endpoints:
- **Liveness Checks**: `/alive` endpoint for container orchestration
- **Readiness Checks**: `/health` endpoint for traffic routing decisions
- **Service-Specific Health**: Custom health checks for each containerized service

### Production Considerations

#### Security Hardening:
- **Change Default Keys**: Update LiteLLM master key and n8n credentials
- **Secure API Keys**: Use Azure Key Vault or similar for production secrets
- **CORS Restrictions**: Configure specific allowed origins instead of wildcard
- **Database Security**: Implement proper PostgreSQL security and TLS
- **Network Policies**: Implement proper container network isolation

#### Scalability & Performance:
- **Container Orchestration**: Ready for Kubernetes deployment
- **Database Optimization**: PostgreSQL tuning for production workloads
- **Caching Strategy**: Redis integration available for session management
- **Load Balancing**: YARP can be configured for multiple backend instances

#### Deployment Options:
- **Docker Compose**: Available override configuration for development
- **Kubernetes**: Aspire can generate K8s manifests
- **Azure Container Apps**: Native Aspire deployment target
- **AWS/GCP**: Container-based deployment with proper networking

### Troubleshooting

#### Common Issues:
- **Service Startup Order**: Check health checks and dependency configuration
- **Port Conflicts**: Verify no other services are using configured ports
- **Authentication Issues**: Validate Azure AD configuration and callback URLs
- **Model Loading**: Ensure Ollama has sufficient resources for model downloads
- **Database Connections**: Check PostgreSQL credentials and network connectivity

#### Debugging Tools:
- **Aspire Dashboard**: Real-time service monitoring and log aggregation
- **Docker Logs**: Individual container log inspection
- **Health Check Endpoints**: Service-specific health validation
- **SSH Access**: Direct container debugging via development environment

### Version Information

#### Framework Versions:
- **.NET**: 9.0 (latest LTS)
- **Aspire**: 9.3.1
- **YARP**: 2.3.0
- **OpenTelemetry**: 1.12.0
- **YamlDotNet**: 16.3.0

#### Container Images:
- **OpenWebUI**: `ghcr.io/open-webui/open-webui:latest`
- **LiteLLM**: `ghcr.io/berriai/litellm-database:main-v1.74.8-nightly`
- **Ollama**: `ollama/ollama:latest`
- **PostgreSQL**: Standard Aspire PostgreSQL image
- **n8n**: `docker.n8n.io/n8nio/n8n:latest`
- **Development**: `ubuntu:24.04` with custom tooling

This comprehensive architecture provides a production-ready AI development platform with enterprise-grade security, monitoring, and scalability features.