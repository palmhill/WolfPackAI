// Node.js Development Example
console.log('Node.js Development Example');

// Example: Working with arrays and modern JS features
const numbers = [5, 2, 8, 1, 9, 3];

console.log('Original:', numbers);

// Modern JS array methods
const doubled = numbers.map(n => n * 2);
const filtered = numbers.filter(n => n > 3);
const sum = numbers.reduce((acc, n) => acc + n, 0);

console.log('Doubled:', doubled);
console.log('Filtered (> 3):', filtered);
console.log('Sum:', sum);

// Async/await example
async function delayedGreeting() {
    return new Promise(resolve => {
        setTimeout(() => resolve('Hello from async Node.js!'), 1000);
    });
}

delayedGreeting().then(console.log);

// To run: node example.js