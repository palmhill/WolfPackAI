# OpenWebUILiteLLM

An integrated solution for deploying [OpenWebUI](https://github.com/open-webui/open-webui) with [LiteLLM](https://github.com/berriai/litellm) backend, using .NET Aspire for orchestration.

## Features

- **Containerized Architecture**: Uses Docker containers for OpenWebUI, LiteLLM, PostgreSQL, and Redis
- **Azure AD Integration**: Authentication using Azure Active Directory
- **Centralized Configuration**: All settings managed through appsettings.json
- **Dynamic YAML Generation**: Automatically generates litellm-config.yaml from configuration
- **Validation**: Built-in configuration validation at startup

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/products/docker-desktop)
- Azure AD tenant (for authentication)
- Access to Azure OpenAI or other LLM APIs

## Configuration

All configuration is managed through the `appsettings.json` file in the `OpenWebUILiteLLM.AppHost` project:
{
  "LiteLLM": {
    "ModelList": [
      {
        "ModelName": "gpt-4",
        "LiteLLMParams": {
          "Model": "azure/gpt-4",
          "ApiBase": "https://your-resource.openai.azure.com/",
          "ApiKey": "AZURE_API_KEY",
          "ApiVersion": "2024-02-15-preview"
        }
      }
    ],
    "Settings": {
      "DropParams": true,
      "SetVerbose": true
    },
    "GeneralSettings": {
      "MasterKey": "sk-1234",
      "DatabaseUrl": "postgresql://postgres:postgres@postgres:5432/litellm"
    },
    "RouterSettings": {
      "RoutingStrategy": "least-busy",
      "NumRetries": 3,
      "Timeout": 600
    },
    "Auth": {
      "AzureAd": {
        "TenantId": "your-tenant-id",
        "ClientId": "your-client-id",
        "ClientSecret": "your-client-secret"
      }
    }
  }
}
## Running the Application

1. Configure the `appsettings.json` file with your Azure AD and LLM API settings
2. Run the application using:
dotnet run --project OpenWebUILiteLLM.AppHost
3. The Aspire dashboard will open, showing the status of all services
4. Access OpenWebUI through the reverse proxy at http://localhost:5000

## Authentication

The application uses Azure AD for authentication. Configure your Azure AD tenant and update the following settings:
"Auth": {
  "AzureAd": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
## Security Considerations

- Change the default `MasterKey` in production
- Secure your API keys and credentials
- Use environment secrets for sensitive information

## Customization

- Add additional models in the `ModelList` array
- Adjust routing strategy and other LiteLLM settings
- Modify the OpenWebUI environment variables in `Program.cs`