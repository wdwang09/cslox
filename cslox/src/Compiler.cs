namespace cslox;

public class Compiler
{
    private readonly Scanner _scanner = new();

    internal InterpretResult Compile(string source)
    {
        _scanner.Init(source);
        var line = -1;
        while (true)
        {
            var token = _scanner.ScanToken();
            if (token.Line != line)
            {
                Console.Write($"{token.Line,4} ");
                line = token.Line;
            }
            else
            {
                Console.Write("   | ");
            }

            Console.WriteLine($"{token.Type} {token.Lexeme}");
            if (token.Type == TokenType.Eof) break;
        }

        return InterpretResult.Ok;
    }
}