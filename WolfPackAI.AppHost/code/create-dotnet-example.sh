#!/bin/bash
# Script to create a .NET example project

echo "Creating .NET example project..."

# Create a new console application
dotnet new console -n DotNetExample -f net9.0

# Replace the default Program.cs with a more interesting example
cat > DotNetExample/Program.cs << 'EOF'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

Console.WriteLine(".NET Development Example");

// Example: Working with LINQ and async
var numbers = new List<int> { 5, 2, 8, 1, 9, 3 };

Console.WriteLine($"Original: [{string.Join(", ", numbers)}]");

// LINQ examples
var doubled = numbers.Select(n => n * 2);
var filtered = numbers.Where(n => n > 3);
var sum = numbers.Sum();

Console.WriteLine($"Doubled: [{string.Join(", ", doubled)}]");
Console.WriteLine($"Filtered (> 3): [{string.Join(", ", filtered)}]");
Console.WriteLine($"Sum: {sum}");

// Class example
public class Calculator
{
    private readonly List<string> _history = new();

    public int Add(int a, int b)
    {
        var result = a + b;
        _history.Add($"{a} + {b} = {result}");
        return result;
    }

    public void ShowHistory()
    {
        Console.WriteLine($"History: [{string.Join(", ", _history)}]");
    }
}

var calc = new Calculator();
var result = calc.Add(10, 20);
Console.WriteLine($"Calculator result: {result}");
calc.ShowHistory();

// Async example
await AsyncExample();

static async Task AsyncExample()
{
    await Task.Delay(100);
    Console.WriteLine("Hello from async .NET!");
}
EOF

echo "✓ .NET example project created!"
echo "To run: cd DotNetExample && dotnet run"