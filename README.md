# CsLox

Use C# to write the Lox bytecode virtual machine in [*Crafting Interpreters*](https://craftinginterpreters.com).

Details are in [Part III](https://craftinginterpreters.com/a-bytecode-virtual-machine.html) of the book.

Command line usage:

```
cd cslox

# REPL
dotnet run
dotnet run --configuration Release

# FILE
dotnet run code.lox
dotnet run --configuration Release code.lox
```

Test (for compilation error or runtime error):

```
# cd <solution directory>
dotnet test
```
