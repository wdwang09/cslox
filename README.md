# CsLox

Use C# to write the Lox bytecode virtual machine in [*Crafting Interpreters*](https://craftinginterpreters.com).

Details are in [Part III](https://craftinginterpreters.com/a-bytecode-virtual-machine.html) of the book.

Command line usage:

```
cd cslox

# REPL
dotnet run
dotnet run -c release

# FILE
dotnet run code.lox
dotnet run -c release code.lox
```

Test (for compilation error or runtime error):

```
# cd <solution directory>
dotnet test
```

For a WebAssembly-based compiler, use Visual Studio or Jetbrains Rider to build, run and publish project `cslox_blazor`.

Currently `cslox_blazor` is a demo project, which means that you should use Console in Browser Developer Tools to show code result.
