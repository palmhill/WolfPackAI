#include <iostream>
#include <vector>
#include <algorithm>

int main() {
    std::cout << "C++ Development Example" << std::endl;
    
    // Example: Working with vectors
    std::vector<int> numbers = {5, 2, 8, 1, 9, 3};
    
    std::cout << "Original: ";
    for (int n : numbers) {
        std::cout << n << " ";
    }
    std::cout << std::endl;
    
    std::sort(numbers.begin(), numbers.end());
    
    std::cout << "Sorted: ";
    for (int n : numbers) {
        std::cout << n << " ";
    }
    std::cout << std::endl;
    
    return 0;
}

// To compile and run:
// g++ example-cpp.cpp -o example-cpp && ./example-cpp