# .NET Aspire OpenWebUI + LiteLLM with Azure AD

## Prerequisites
- .NET 8.0 SDK
- Docker Desktop
- Azure subscription with Azure OpenAI deployed
- Azure AD tenant

## Setup Instructions

1. **Clone and Initialize**
   ```bash
   dotnet new aspire-starter -n OpenWebUILiteLLM
   cd OpenWebUILiteLLM
   ```

2. **Configure Azure AD**
   - Register an application in Azure AD
   - Note the Tenant ID, Client ID, and create a Client Secret
   - Configure redirect URIs for your domain

3. **Configure Environment**
   Create `.env` file:
   ```
   AZURE_AD_TENANT_ID=your-tenant-id
   AZURE_AD_CLIENT_ID=your-client-id
   AZURE_AD_CLIENT_SECRET=your-client-secret
   AZURE_OPENAI_API_KEY=your-api-key
   AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
   ```

4. **Update Configuration**
   - Update `litellm-config.yaml` with your Azure OpenAI endpoints
   - Update model deployments to match your Azure OpenAI instance

5. **Run the Application**
   ```bash
   dotnet run --project OpenWebUILiteLLM.AppHost
   ```

6. **Access Services**
   - Open-WebUI: http://localhost:8080
   - LiteLLM API: http://localhost:4000
   - Aspire Dashboard: http://localhost:15888

## Architecture

- **Open-WebUI**: Provides the user interface for interacting with LLMs
- **LiteLLM**: Acts as a proxy to standardize access to various LLM providers
- **PostgreSQL**: Stores user data, conversations, and configuration
- **Redis**: Handles session management and caching
- **Reverse Proxy**: (Optional) Provides unified authentication and routing

## Security Considerations

1. Change default master keys in production
2. Use Azure Key Vault for secrets management
3. Enable HTTPS in production
4. Configure proper CORS policies
5. Implement rate limiting
6. Regular security updates for containers

## Troubleshooting

- Check Aspire dashboard for service health
- Verify Azure AD configuration in app registrations
- Ensure Azure OpenAI endpoints are accessible
- Check container logs for detailed error messages