# Gnibo Programming Language Documentation

## 📌 Overview

Gnibo is a low-level, expression-based programming language that compiles to x86-64 Linux assembly. It supports variables, arithmetic, control flow, functions, logical operations, and string handling. It is designed for simplicity and closeness to hardware behavior. Gnibo files use the `.nob` file extension.

---

## ✨ Features

* Variables (`let` declarations)
* Integer arithmetic
* Functions with return values and parameters
* Conditional statements (`if`, `else`, `&&`, `||`)
* Comparison operators: `>`, `<`, `>=`, `<=`, `==`, `!=`
* Loops (`while`, `for`) with `break` and `continue`
* Built-in functions: `print`, `exit`
* String literals and concatenation
* Automatic number-to-string conversion in string concatenation
* Explicit `.toString()` conversion for numbers
* Comments: `//` for single-line, `/* ... */` for multi-line
* Lexical scoping

---

## 📚 Syntax

### Variables

```gnibo
let x = 5;
let y = x + 3;
```

### Functions

```gnibo
fn add(a, b) {
    return a + b;
}
let result = add(3, 4);
```

### Conditionals

```gnibo
if(x > 5) print("big");
else print("small");

if(a > 0 && b < 5) print("OK");
```

### Loops

```gnibo
let i = 5;
while(i > 0) {
    print(i);
    if(i == 3) break;
    i = i - 1;
}
```

### Comments

```gnibo
// This is a single-line comment

/*
This is a
multi-line comment
*/
```

### Built-in Functions

#### `print()`

* Accepts strings, integers, and expressions with `.toString()`
* Supports string concatenation

```gnibo
print("Value: " + 42);
```

#### `exit(code)`

* Ends the program with return code `code`

```gnibo
exit(0);
```

---

## 🔤 Strings

* String literals are supported inside `print()`
* Concatenation is done using `+`
* Numbers are automatically cast to strings when concatenated with a string
* You can also explicitly convert numbers using `.toString()`

```gnibo
print("Number: " + 5);
print("Sum: " + (3 + 2).toString());
```

> Note: Currently, assigning string values to variables is not supported.

---

## 🧠 Expression Behavior

* Parentheses are respected: `((a + b) * 3).toString()` is valid
* Nested expressions and function calls are supported: `f(g(x))`

---

## 📦 Known Limitations

* No string variable storage yet
* No file I/O
* No standard library
* No type system
* Only integer and string (as literals) types are supported

---

## 🛠 Running a Gnibo File

```bash
gnibo main.nob
```

Make sure `gnibo.bat` or the executable is in your system PATH.

### Example `gnibo.bat` (Windows)

```bat
@echo off
dotnet run --project "The folder that your file is in" -- %1
```

---

## ✍ Example Program

```gnibo
fn square(n) {
    return n * n;
}

let x = 9;
print("Square of x: " + square(x).toString());
exit(0);
```

---

## 📅 Roadmap

* [ ] String variables (currently variables are added. But only them, no reassignment, concatenation or anything.)
* [ ] More standard functions (e.g., `length()`, `input()`)
* [ ] Type inference or hints
* [ ] Object.method syntax (`x.toString()` for all values)
