namespace cslox;

public class Compiler
{
    private readonly Dictionary<TokenType, ParseRule> _rules;
    private readonly Vm _vm = new();
    private Chunk? _chunk;
    private Parser? _parser;
    private Scanner? _scanner;

    public Compiler()
    {
        _rules = new Dictionary<TokenType, ParseRule>
        {
            [TokenType.LeftParen] = new(Grouping, null, Precedence.None),
            [TokenType.RightParen] = new(null, null, Precedence.None),
            [TokenType.LeftBrace] = new(null, null, Precedence.None),
            [TokenType.RightBrace] = new(null, null, Precedence.None),
            [TokenType.Comma] = new(null, null, Precedence.None),
            [TokenType.Dot] = new(null, null, Precedence.None),
            [TokenType.Minus] = new(Unary, Binary, Precedence.Term),
            [TokenType.Plus] = new(null, Binary, Precedence.Term),
            [TokenType.Semicolon] = new(null, null, Precedence.None),
            [TokenType.Slash] = new(null, Binary, Precedence.Factor),
            [TokenType.Star] = new(null, Binary, Precedence.Factor),
            [TokenType.Bang] = new(null, null, Precedence.None),
            [TokenType.BangEqual] = new(null, null, Precedence.None),
            [TokenType.Equal] = new(null, null, Precedence.None),
            [TokenType.EqualEqual] = new(null, null, Precedence.None),
            [TokenType.Greater] = new(null, null, Precedence.None),
            [TokenType.GreaterEqual] = new(null, null, Precedence.None),
            [TokenType.Less] = new(null, null, Precedence.None),
            [TokenType.LessEqual] = new(null, null, Precedence.None),
            [TokenType.Identifier] = new(null, null, Precedence.None),
            [TokenType.String] = new(null, null, Precedence.None),
            [TokenType.Number] = new(Number, null, Precedence.None),
            [TokenType.And] = new(null, null, Precedence.None),
            [TokenType.Class] = new(null, null, Precedence.None),
            [TokenType.Else] = new(null, null, Precedence.None),
            [TokenType.False] = new(null, null, Precedence.None),
            [TokenType.For] = new(null, null, Precedence.None),
            [TokenType.Fun] = new(null, null, Precedence.None),
            [TokenType.If] = new(null, null, Precedence.None),
            [TokenType.Nil] = new(null, null, Precedence.None),
            [TokenType.Or] = new(null, null, Precedence.None),
            [TokenType.Print] = new(null, null, Precedence.None),
            [TokenType.Return] = new(null, null, Precedence.None),
            [TokenType.Super] = new(null, null, Precedence.None),
            [TokenType.This] = new(null, null, Precedence.None),
            [TokenType.True] = new(null, null, Precedence.None),
            [TokenType.Var] = new(null, null, Precedence.None),
            [TokenType.While] = new(null, null, Precedence.None),
            [TokenType.Error] = new(null, null, Precedence.None),
            [TokenType.Eof] = new(null, null, Precedence.None)
        };
    }

    internal bool Compile(string source)
    {
        _scanner = new Scanner(source);
        _chunk = new Chunk();
        _parser = new Parser();
        Advance();
        Expression();
        Consume(TokenType.Eof, "Expect end of expression.");
        EndCompiler();
        return !_parser.HadError;
    }

    internal InterpretResult Run()
    {
        if (_chunk is null) return InterpretResult.CompileError;
        return _vm.Interpret(_chunk);
    }

    private void Advance()
    {
        _parser!.Previous = _parser.Current;
        while (true)
        {
            var token = _scanner!.ScanToken();
            _parser.Current = token;
            if (token.Type != TokenType.Error) break;
            _parser.ErrorAtCurrent(token.Lexeme);
        }
    }

    private void Expression()
    {
        ParsePrecedence(Precedence.Assignment);
    }

    private void ParsePrecedence(Precedence precedence)
    {
        Advance();
        var prefixRule = _rules[_parser!.Previous!.Value.Type].Prefix;
        if (prefixRule is null)
        {
            _parser.Error("Expect expression.");
            return;
        }

        prefixRule();

        while (precedence <= _rules[_parser!.Current!.Value.Type].Precedence)
        {
            Advance();
            var infixRule = _rules[_parser!.Previous!.Value.Type].Infix;
            infixRule!();
        }
    }

    private void Consume(TokenType type, string message)
    {
        if (_parser!.Current!.Value.Type == type)
        {
            Advance();
            return;
        }

        _parser.ErrorAtCurrent(message);
    }

    private void EmitByte(byte @byte)
    {
        _chunk!.WriteChunk(@byte, _parser!.Previous!.Value.Line);
    }

    private void EmitBytes(byte byte1, byte byte2)
    {
        EmitByte(byte1);
        EmitByte(byte2);
    }

    private void EndCompiler()
    {
        EmitReturn();
#if DEBUG
        if (!_parser!.HadError) _chunk!.DisassembleChunk("code");
#endif
    }

    private void EmitReturn()
    {
        EmitByte((byte)OpCode.Return);
    }

    private void Number()
    {
        var value = double.Parse(_parser!.Previous!.Value.Lexeme);
        EmitConstant(value);
    }

    private void EmitConstant(double value)
    {
        // double -> Value
        EmitBytes((byte)OpCode.Constant, MakeConstant(value));
    }

    private byte MakeConstant(double value)
    {
        var constant = _chunk!.AddConstant(value);
        if (constant > byte.MaxValue)
        {
            _parser!.Error("Too many constants in one chunk.");
            return 0;
        }

        return (byte)constant;
    }

    private void Grouping()
    {
        Expression();
        Consume(TokenType.RightParen, "Expect ')' after expression.");
    }

    private void Unary()
    {
        var operatorType = _parser!.Previous!.Value.Type;
        ParsePrecedence(Precedence.Unary);
        switch (operatorType)
        {
            case TokenType.Minus:
                EmitByte((byte)OpCode.Negate);
                break;
            default:
                return;
        }
    }

    private void Binary()
    {
        var operatorType = _parser!.Previous!.Value.Type;
        var rule = _rules[operatorType];
        ParsePrecedence(rule.Precedence + 1);
        switch (operatorType)
        {
            case TokenType.Plus:
                EmitByte((byte)OpCode.Add);
                break;
            case TokenType.Minus:
                EmitByte((byte)OpCode.Subtract);
                break;
            case TokenType.Star:
                EmitByte((byte)OpCode.Multiply);
                break;
            case TokenType.Slash:
                EmitByte((byte)OpCode.Divide);
                break;
            default:
                return;
        }
    }
}