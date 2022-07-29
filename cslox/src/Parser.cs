namespace cslox;

internal enum Precedence
{
    None,
    Assignment, // =
    Or, // or
    And, // and
    Equality, // == !=
    Comparison, // < > <= >=
    Term, // + -
    Factor, // * /
    Unary, // ! -
    Call, // . ()
    Primary
}

internal delegate void ParseFn();

internal readonly struct ParseRule
{
    internal ParseRule(ParseFn? prefix, ParseFn? infix, Precedence precedence)
    {
        Prefix = prefix;
        Infix = infix;
        Precedence = precedence;
    }

    internal ParseFn? Prefix { get; }
    internal ParseFn? Infix { get; }
    internal Precedence Precedence { get; }
}

public class Parser
{
    private bool _panicMode;
    internal Token? Current;
    internal bool HadError;
    internal Token? Previous;

    internal Parser()
    {
        Current = null;
        Previous = null;
        HadError = false;
        _panicMode = false;
    }

    internal void ErrorAtCurrent(string message)
    {
        // if (Current is null) throw new Exception("Shouldn't happen.");
        ErrorAt(Current!.Value, message);
    }

    internal void Error(string message)
    {
        // if (Previous is null) throw new Exception("Shouldn't happen.");
        ErrorAt(Previous!.Value, message);
    }

    private void ErrorAt(Token token, string message)
    {
        if (_panicMode) return;
        _panicMode = true;
        Console.Error.Write($"[Line {token.Line}]");
        if (token.Type == TokenType.Eof)
        {
            Console.Error.Write(" at end");
        }
        else if (token.Type == TokenType.Error)
        {
            // Nothing.
        }
        else
        {
            Console.Error.Write($" at '{token.Lexeme}'");
        }

        Console.Error.WriteLine($": {message}");
        HadError = true;
    }
}