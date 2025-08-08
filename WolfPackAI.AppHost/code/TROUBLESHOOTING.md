# Development Container Troubleshooting

## Docker Build Issues Resolution

The original issue was related to Aspire's Docker build context handling. Here are the solutions implemented:

### Problem
```
could not build the image {"Container": {"name":"devcontainer-xxxx"}, "error": "could not open the image ID file: ... The system cannot find the file specified"}
```

### Root Causes Identified
1. **Build Context Issues**: Aspire's `AddDockerfile()` method had problems with temporary file handling in WSL2 environment
2. **Build Timeout**: Large builds timing out due to extensive package installations
3. **Path Resolution**: Windows/Linux path mapping issues between WSL2 and Docker

### Solutions Implemented

#### 1. Container Approach (Current)
```csharp
var devContainer = builder.AddContainer("devcontainer", "ubuntu", "24.04")
    .WithEndpoint(targetPort: 22, port: 2222, scheme: "tcp", name: "ssh")
    .WithBindMount("./code", "/app/code")
    .WithEntrypoint("/bin/bash", "-c", "...")  // Install packages at runtime
```

**Pros**: Avoids build context issues, faster startup for debugging
**Cons**: Slower first startup due to runtime package installation

#### 2. Pre-built Image Approach (Recommended for Production)

Build the image separately:
```bash
cd WolfPackAI.AppHost
docker build -f Dockerfile.devcontainer.multistage -t aspire-devcontainer:latest .
```

Then use in Program.cs:
```csharp
var devContainer = builder.AddContainer("devcontainer", "aspire-devcontainer", "latest")
    .WithEndpoint(targetPort: 22, port: 2222, scheme: "tcp", name: "ssh")
    .WithBindMount("./code", "/app/code");
```

#### 3. Dockerfile with Context Fix (Backup)
If you need to use Dockerfile approach:
- Ensure Dockerfile is in the correct build context directory
- Use shorter, optimized build commands
- Consider multi-stage builds for better caching

### Environment Details
- **OS**: WSL2 Ubuntu on Windows
- **Docker**: overlayfs storage driver
- **Aspire**: .NET 9.0
- **Build Context**: /mnt/c/Users/localadmin/source/repos/AspireWolfPackAI/WolfPackAI.AppHost

### Connection Info
Once running:
```bash
ssh root@localhost -p 2222        # password: supersecurepassword
ssh developer@localhost -p 2222   # password: devpassword
```

### Available Tools
- **C/C++**: gcc, g++, cmake, make, build-essential
- **.NET**: SDK 9.0
- **Node.js**: 22.x with npm  
- **Python**: 3.12 with pip and venv
- **Git**: Version control
- **Editors**: vim, nano

### File Sync
- Local: `./code` directory
- Container: `/app/code` directory
- Files are synchronized bidirectionally