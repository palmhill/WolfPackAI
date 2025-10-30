# WolfPackAI Standalone Quickstart

## Prerequisites
1. Install the .NET SDK 9.0 or newer.
2. Install the .NET Aspire workload:
   ```powershell
   dotnet workload install aspire
   ```
3. Install Docker Desktop and ensure it is running.
4. Optional: Install Node.js 22.x and Python 3.x if you plan to extend the embedded services.

## Initial Setup
```powershell
# Navigate to the project
cd C:\Users\SSaint-Cyr\Documents\GitHub\WolfPackAI

# Restore NuGet packages
dotnet restore
```

## Build and Test
```powershell
# Build the solution
dotnet build

# Run automated tests
dotnet test
```

## Run the Distributed Application
```powershell
# Launch the Aspire AppHost
dotnet run --project WolfPackAI.AppHost
```

After the containers start, access the services through the Aspire dashboard (default http://localhost:15021).

## Useful Commands
```powershell
# Clean build artifacts
dotnet clean

# Stop all Aspire-managed containers
# Press Ctrl+C in the AppHost terminal window
```

## Next Steps
- Update `WolfPackAI.AppHost/appsettings.json` with credentials for LiteLLM, PostgreSQL, and OpenWebUI.
- Review the workflows in `WolfPackAI.Dashboard` for customization points.
- Capture any environment-specific secrets using user secrets or environment variables instead of committing them to source control.
