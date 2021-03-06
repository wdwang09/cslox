namespace cslox;

public class CsLox
{
    private readonly Compiler _compiler = new();

    public void Repl()
    {
        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine();
            if (line is null) break;
            Interpret(line);
        }
    }

    public int RunFile(string fileName)
    {
        if (!File.Exists(fileName))
        {
            Console.Error.WriteLine($"Cannot open file \"{fileName}\".");
            return 74;
        }

        var source = File.ReadAllText(fileName);
        var result = Interpret(source);
        return result switch
        {
            InterpretResult.Ok => 0,
            InterpretResult.CompileError => 65,
            InterpretResult.RuntimeError => 70,
            _ => 1
        };
    }

    private InterpretResult Interpret(string source)
    {
        if (!_compiler.Compile(source)) return InterpretResult.CompileError;
        return _compiler.Run();
    }
}