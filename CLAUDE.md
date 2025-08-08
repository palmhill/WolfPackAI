# CLAUDE.md

This file provides comprehensive guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Development Commands

### Build and Run
- **Build the solution**: `dotnet build`
- **Run the main application**: `dotnet run --project OpenWebUILiteLLM.AppHost`
- **Run specific project**: `dotnet run --project <ProjectName>`
- **Clean build artifacts**: `dotnet clean`

### Testing
- **Run all tests**: `dotnet test`
- **Run tests with verbose output**: `dotnet test --verbosity normal`

## Architecture Overview

This is a sophisticated .NET Aspire-orchestrated solution that integrates multiple AI services including OpenWebUI, LiteLLM, Claude Code Router (CCR), and n8n workflow automation. The architecture provides a comprehensive AI development and deployment platform.

### Core Projects
1. **OpenWebUILiteLLM.AppHost** - The main Aspire orchestration host that configures and manages all containerized services
2. **ReverseProxy** - YARP-based reverse proxy that routes requests between services with advanced path rewriting
3. **OpenWebUILiteLLM.ServiceDefaults** - Shared service configuration and extensions for OpenTelemetry, health checks, and resilience

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

All configuration is centralized in `OpenWebUILiteLLM.AppHost/appsettings.json` with strongly-typed configuration classes:

#### Configuration Sections:
- **LiteLLM**: Model configurations, routing settings, and master key
- **Auth**: Azure AD integration settings (tenant, client ID/secret)
- **OpenWebUI**: Public URL configuration for OAuth callbacks
- **Postgres**: Database connection settings and credentials
- **PublicNetwork**: External networking configuration (HTTP/HTTPS ports, public URL)

The system features dynamic YAML generation that converts JSON configuration to LiteLLM-compatible YAML format using custom serialization logic in `Configuration.cs`.

### Key Components

#### Program.cs (AppHost) - `OpenWebUILiteLLM.AppHost/Program.cs`
- **Configuration Loading & Validation**: Validates all service configurations at startup
- **Service Dependency Management**: Sets up proper startup ordering with health checks
- **Container Orchestration**: Configures all Docker containers with environment variables, volumes, and networking
- **Dynamic YAML Generation**: Creates LiteLLM configuration file from appsettings.json
- **Multi-Service Integration**: Manages dependencies between OpenWebUI, LiteLLM, Ollama, PostgreSQL, n8n, and CCR

#### Configuration.cs - `OpenWebUILiteLLM.AppHost/Configuration.cs`
- **Strongly-Typed Models**: Configuration classes with JSON and YAML serialization attributes
- **Validation Logic**: Ensures all required configuration values are present with detailed error messages
- **YAML Serialization**: Converts C# configuration objects to LiteLLM-compatible YAML format using YamlDotNet
- **Environment Variable Integration**: Handles secure API key references via environment variables

#### ReverseProxy/Program.cs - `ReverseProxy/Program.cs`
- **YARP Configuration**: Advanced request routing with path transformation and header management
- **Landing Page**: Serves a responsive HTML portal at `/` with links to all services
- **Response Transformation**: Handles Location header rewriting for proper service routing
- **Service Path Management**: Routes to /chat, /litellm/, /ccr/, /n8n/, and /claude-code/ endpoints
- **Static File Serving**: Serves static assets from wwwroot directory

#### Ollama Integration - `OpenWebUILiteLLM.AppHost/OllamaResource.cs`
- **Custom Resource Definition**: Extends ContainerResource for Ollama-specific functionality
- **Connection String Generation**: Provides connection strings for other services
- **GPU Support**: Configurable GPU acceleration for model inference
- **Model Management**: Handles model downloading and lifecycle management

#### Service Extensions - `OpenWebUILiteLLM.AppHost/ResourceExtensions.cs`
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

### Development Workflow

#### Getting Started:
1. **Configure `appsettings.json`** with your Azure AD tenant and API settings
2. **Run the application**: `dotnet run --project OpenWebUILiteLLM.AppHost`
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