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
            [TokenType.Bang] = new(Unary, null, Precedence.None),
            [TokenType.BangEqual] = new(null, Binary, Precedence.Equality),
            [TokenType.Equal] = new(null, null, Precedence.None),
            [TokenType.EqualEqual] = new(null, Binary, Precedence.Equality),
            [TokenType.Greater] = new(null, Binary, Precedence.Comparison),
            [TokenType.GreaterEqual] = new(null, Binary, Precedence.Comparison),
            [TokenType.Less] = new(null, Binary, Precedence.Comparison),
            [TokenType.LessEqual] = new(null, Binary, Precedence.Comparison),
            [TokenType.Identifier] = new(null, null, Precedence.None),
            [TokenType.String] = new(String, null, Precedence.None),
            [TokenType.Number] = new(Number, null, Precedence.None),
            [TokenType.And] = new(null, null, Precedence.None),
            [TokenType.Class] = new(null, null, Precedence.None),
            [TokenType.Else] = new(null, null, Precedence.None),
            [TokenType.False] = new(Literal, null, Precedence.None),
            [TokenType.For] = new(null, null, Precedence.None),
            [TokenType.Fun] = new(null, null, Precedence.None),
            [TokenType.If] = new(null, null, Precedence.None),
            [TokenType.Nil] = new(Literal, null, Precedence.None),
            [TokenType.Or] = new(null, null, Precedence.None),
            [TokenType.Print] = new(null, null, Precedence.None),
            [TokenType.Return] = new(null, null, Precedence.None),
            [TokenType.Super] = new(null, null, Precedence.None),
            [TokenType.This] = new(null, null, Precedence.None),
            [TokenType.True] = new(Literal, null, Precedence.None),
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

    private void EmitByte(OpCode @byte)
    {
        _chunk!.WriteChunk((byte)@byte, _parser!.Previous!.Value.Line);
    }

    private void EmitBytes(OpCode byte1, byte byte2)
    {
        EmitByte(byte1);
        EmitByte(byte2);
    }

    private void EmitBytes(OpCode byte1, OpCode byte2)
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
        EmitByte(OpCode.Return);
    }

    private void Number()
    {
        var value = double.Parse(_parser!.Previous!.Value.Lexeme);
        EmitConstant(new Value(value));
    }

    private void EmitConstant(Value value)
    {
        EmitBytes(OpCode.Constant, MakeConstant(value));
    }

    private byte MakeConstant(Value value)
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
            case TokenType.Bang:
                EmitByte(OpCode.Not);
                break;
            case TokenType.Minus:
                EmitByte(OpCode.Negate);
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
            case TokenType.BangEqual:
                EmitBytes(OpCode.Equal, OpCode.Not);
                break;
            case TokenType.EqualEqual:
                EmitByte(OpCode.Equal);
                break;
            case TokenType.Greater:
                EmitByte(OpCode.Greater);
                break;
            case TokenType.GreaterEqual:
                EmitBytes(OpCode.Less, OpCode.Not);
                break;
            case TokenType.Less:
                EmitByte(OpCode.Less);
                break;
            case TokenType.LessEqual:
                EmitBytes(OpCode.Greater, OpCode.Not);
                break;
            case TokenType.Plus:
                EmitByte(OpCode.Add);
                break;
            case TokenType.Minus:
                EmitByte(OpCode.Subtract);
                break;
            case TokenType.Star:
                EmitByte(OpCode.Multiply);
                break;
            case TokenType.Slash:
                EmitByte(OpCode.Divide);
                break;
            default:
                return;
        }
    }

    private void Literal()
    {
        switch (_parser!.Previous!.Value.Type)
        {
            case TokenType.False:
                EmitByte(OpCode.False);
                break;
            case TokenType.Nil:
                EmitByte(OpCode.Nil);
                break;
            case TokenType.True:
                EmitByte(OpCode.True);
                break;
            default:
                return;
        }
    }

    private void String()
    {
        var str = _parser!.Previous!.Value.Lexeme;
        var obj = new ObjString(str.Substring(1, str.Length - 2));
        EmitConstant(new Value(obj));
    }
}