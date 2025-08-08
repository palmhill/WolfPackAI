#!/usr/bin/env python3
"""
Python Development Example
"""

import asyncio
from typing import List

def main():
    print("Python Development Example")
    
    # Example: Working with lists and comprehensions
    numbers = [5, 2, 8, 1, 9, 3]
    
    print(f"Original: {numbers}")
    
    # List comprehensions
    doubled = [n * 2 for n in numbers]
    filtered = [n for n in numbers if n > 3]
    total = sum(numbers)
    
    print(f"Doubled: {doubled}")
    print(f"Filtered (> 3): {filtered}")
    print(f"Sum: {total}")
    
    # Class example
    class Calculator:
        def __init__(self):
            self.history = []
        
        def add(self, a: int, b: int) -> int:
            result = a + b
            self.history.append(f"{a} + {b} = {result}")
            return result
    
    calc = Calculator()
    result = calc.add(10, 20)
    print(f"Calculator result: {result}")
    print(f"History: {calc.history}")

async def async_example():
    """Async function example"""
    await asyncio.sleep(0.1)
    print("Hello from async Python!")

if __name__ == "__main__":
    main()
    asyncio.run(async_example())

# To run: python3 example.py