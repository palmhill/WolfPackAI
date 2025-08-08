# Development Container Code Directory

This directory is synchronized with the development container's `/app/code` directory.

## Usage

1. Start the Aspire application: `dotnet run --project WolfPackAI.AppHost`
2. SSH into the development container: `ssh root@localhost -p 2222` (password: supersecurepassword)
   - Or use the developer account: `ssh developer@localhost -p 2222` (password: devpassword)
3. Navigate to `/app/code` in the container to work with synced files
4. Files created/modified in this local `./code` directory will appear in the container
5. Files created/modified in the container's `/app/code` will appear here locally

## Available Development Tools

- **C/C++**: gcc, g++, cmake, make, libtool, autoconf, automake
- **.NET**: .NET SDK 9.0
- **Node.js**: Node.js 22.x with npm
- **Python**: Python 3.12 with pip and venv
- **Git**: Version control
- **Editors**: vim, nano

## Example Workflow

```bash
# SSH into the container
ssh developer@localhost -p 2222

# In the container, navigate to the code directory
cd /app/code

# Create a C++ project
mkdir my-cpp-project && cd my-cpp-project
echo '#include <iostream>\nint main() { std::cout << "Hello from dev container!"; return 0; }' > main.cpp
g++ main.cpp -o hello && ./hello

# Create a .NET project  
dotnet new console -n MyDotNetApp
cd MyDotNetApp && dotnet run

# Create a Node.js project
mkdir my-node-project && cd my-node-project
npm init -y
npm install express
echo 'console.log("Hello from Node.js!");' > app.js
node app.js
```

Files will automatically sync between the container and your local machine for easy editing with your preferred IDE.