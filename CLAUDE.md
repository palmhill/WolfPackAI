# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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

This is a .NET Aspire-orchestrated solution that integrates OpenWebUI with LiteLLM backend using containerized services. The architecture consists of:

### Core Projects
1. **OpenWebUILiteLLM.AppHost** - The main Aspire orchestration host that configures and manages all containerized services
2. **ReverseProxy** - YARP-based reverse proxy that routes requests between services  
3. **OpenWebUILiteLLM.ServiceDefaults** - Shared service configuration and extensions

### Service Architecture
The application orchestrates multiple Docker containers:
- **OpenWebUI** (ghcr.io/open-webui/open-webui:latest) - AI chat interface on port 8080
- **LiteLLM** (ghcr.io/berriai/litellm-database:main-v1.74.8-nightly) - LLM proxy/gateway on port 4000
- **Ollama** - Local LLM runtime (qwen3:0.6b model) on port 1143
- **PostgreSQL** - Database for all services (openwebuidb, litellmdb, n8ndb databases)
- **n8n** - Workflow automation platform on port 5678
- **Reverse Proxy** - Routes external traffic to internal services

### Configuration System
All configuration is centralized in `OpenWebUILiteLLM.AppHost/appsettings.json`:

- **LiteLLM**: Model configurations, routing settings, and master key
- **Auth**: Azure AD integration settings (tenant, client ID/secret)
- **OpenWebUI**: Public URL configuration
- **Postgres**: Database connection settings
- **PublicNetwork**: External networking configuration

The system dynamically generates `litellm-config.yaml` from the JSON configuration using custom serialization logic in `Configuration.cs`.

### Key Components

#### Program.cs (AppHost)
- **Configuration Loading**: Validates and loads all service configurations
- **Service Dependencies**: Sets up proper startup ordering with health checks
- **Container Orchestration**: Configures all Docker containers with environment variables and volumes
- **Dynamic YAML Generation**: Creates LiteLLM configuration file from appsettings.json

#### Configuration.cs
- **Configuration Models**: Strongly-typed configuration classes with validation
- **YAML Generation**: Converts C# configuration objects to LiteLLM-compatible YAML format
- **Validation Logic**: Ensures all required configuration values are present

#### ReverseProxy/Program.cs
- **YARP Configuration**: Routes external requests to internal services
- **Landing Page**: Serves a static HTML portal at `/` with links to all services
- **Response Transformation**: Handles redirect rewriting for proper service routing
- **Location Header Processing**: Manages URL transformations for backend services
- **Static File Serving**: Serves static assets from wwwroot directory

### Authentication Flow
The system uses Azure Active Directory for authentication:
1. OpenWebUI configured with OAuth OIDC provider pointing to Azure AD
2. OAuth callback URL automatically constructed based on public URL configuration
3. User authentication handled entirely through Azure AD with email-based account merging

### Data Persistence
- PostgreSQL serves as the primary database for all services
- Separate databases created for each service (openwebuidb, litellmdb, n8ndb)
- Docker volumes maintain data persistence across container restarts
- Health checks ensure database availability before service startup

### Development Workflow
1. Configure `appsettings.json` with your Azure AD and LLM API settings
2. Run `dotnet run --project OpenWebUILiteLLM.AppHost`
3. Access Aspire dashboard to monitor service health
4. Access the landing page at `http://localhost:5000/` for service navigation
5. Individual services available at:
   - **Landing Page**: `http://localhost:5000/`
   - **OpenWebUI**: `http://localhost:5000/chat`
   - **LiteLLM**: `http://localhost:5000/litellm/`
   - **n8n**: `http://localhost:5000/n8n/`

### Security Considerations
- Master key for LiteLLM should be changed in production
- Azure AD client secrets should be stored securely (consider using user secrets: `dotnet user-secrets`)
- Database passwords should be secured in production environments
- CORS settings are permissive for development but should be restricted in production