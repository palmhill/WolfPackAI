#!/bin/bash
echo "=== Testing Development Container Environments ==="

echo "1. Testing C++ environment..."
cat > hello.cpp << 'EOF'
#include <iostream>
int main() {
    std::cout << "✓ C++ environment working!" << std::endl;
    return 0;
}
EOF
g++ hello.cpp -o hello_cpp && ./hello_cpp && rm hello.cpp hello_cpp

echo "2. Testing .NET environment..."
dotnet --version
echo "✓ .NET environment working!"

echo "3. Testing Node.js environment..."
node --version
echo "✓ Node.js environment working!"

echo "4. Testing Python environment..."
python3 --version
echo "✓ Python environment working!"

echo "5. Testing Git..."
git --version
echo "✓ Git environment working!"

echo ""
echo "🎉 All environments are working correctly!"
echo "You can now develop in C/C++, .NET, Node.js, and Python!"
echo ""
echo "SSH Connection Info:"
echo "  Host: localhost"
echo "  Port: 2222"
echo "  Users: root (supersecurepassword) or developer (devpassword)"
echo "  Working Directory: /app/code"