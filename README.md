# LL(1) Parser

A console-based LL(1) parser implemented in C# (.NET 6). Given a context-free grammar, the tool computes **FIRST** and **FOLLOW** sets, builds the **LL(1) parsing table**, validates whether the grammar is LL(1), and interactively parses input strings while displaying a **parse tree**.

## Features

- Reads an arbitrary context-free grammar from the console
- Computes FIRST and FOLLOW sets
- Builds and displays the LL(1) parsing table
- Validates LL(1) conditions and reports conflicts
- Parses input strings step-by-step (stack trace)
- Displays a visual parse tree for accepted strings

## Requirements

- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)

## Getting Started

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run --project CompilerProject
```

## Usage

1. Enter the number of productions.
2. Enter each production in the format `A=α/β/...` where:
- The left-hand side is a single uppercase letter (non-terminal).
- Productions are separated by `/`.
- Use `ε` for epsilon (empty string).
- The **first** production's left-hand side is treated as the start symbol.
3. The program will display FIRST/FOLLOW sets and the parsing table.
4. If the grammar is LL(1), enter strings to parse. Type `exit` to quit.

### Example

```
Enter number of productions: 3
Production: E=TQ
Production: Q=+TQ/ε
Production: T=id
```

**Terminals** are represented by lowercase letters or symbols (anything that is not an uppercase letter).

## Project Structure

```
LL1Parser.sln
├── CompilerProject/
│   ├── LL1Parser.csproj
│   └── Program.cs
└── README.md
??? README.md
```
