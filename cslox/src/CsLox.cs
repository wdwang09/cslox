namespace cslox;

public class CsLox
{
    private readonly CompilerSystem _compilerSystem = new();

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
        return RunCode(source);
    }
    
    public int RunCode(string code)
    {
        var result = Interpret(code);
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
        return _compilerSystem.CompileAndRun(source);
    }
}