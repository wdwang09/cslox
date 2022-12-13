namespace cslox;

internal enum TokenType
{
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    Comma,
    Dot,
    Minus,
    Plus,
    Semicolon,
    Slash,
    Star,

    // One or two character tokens.
    Bang,
    BangEqual,
    Equal,
    EqualEqual,
    Greater,
    GreaterEqual,
    Less,
    LessEqual,

    // Literals.
    Identifier,
    String,
    Number,

    // Keywords.
    And,
    Class,
    Else,
    False,
    For,
    Fun,
    If,
    Nil,
    Or,
    Print,
    Return,
    Super,
    This,
    True,
    Var,
    While,

    Error,
    Eof
}

internal readonly struct Token
{
    internal Token(TokenType type, string lexeme, int line)
    {
        Type = type;
        Lexeme = lexeme;
        Line = line;
    }

    internal TokenType Type { get; }
    internal string Lexeme { get; }
    internal int Line { get; }
}

public class Scanner
{
    private readonly string _source;
    private int _current;
    private int _line;
    private int _start;

    internal Scanner(string source)
    {
        _source = source;
        _start = 0;
        _current = 0;
        _line = 1;
    }

    internal Token ScanToken()
    {
        SkipWhitespace();
        _start = _current;
        if (IsAtEnd()) return MakeToken(TokenType.Eof);
        var c = Advance();
        if (IsAlpha(c)) return IdentifierToken();
        if (char.IsDigit(c)) return NumberToken();
        switch (c)
        {
            case '(':
                return MakeToken(TokenType.LeftParen);
            case ')':
                return MakeToken(TokenType.RightParen);
            case '{':
                return MakeToken(TokenType.LeftBrace);
            case '}':
                return MakeToken(TokenType.RightBrace);
            case ';':
                return MakeToken(TokenType.Semicolon);
            case ',':
                return MakeToken(TokenType.Comma);
            case '.':
                return MakeToken(TokenType.Dot);
            case '-':
                return MakeToken(TokenType.Minus);
            case '+':
                return MakeToken(TokenType.Plus);
            case '/':
                return MakeToken(TokenType.Slash);
            case '*':
                return MakeToken(TokenType.Star);
            case '!':
                return MakeToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);
            case '=':
                return MakeToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
            case '<':
                return MakeToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
            case '>':
                return MakeToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
            case '"':
                return StringToken();
        }

        return ErrorToken("Unexpected character.");
    }

    private void SkipWhitespace()
    {
        while (true)
        {
            var c = Peek();
            switch (c)
            {
                case ' ':
                case '\r':
                case '\t':
                    Advance();
                    break;
                case '\n':
                    _line++;
                    Advance();
                    break;
                case '/':
                    if (PeekNext() == '/')
                        while (Peek() != '\n' && !IsAtEnd())
                            Advance();
                    else
                        return;
                    break;
                default:
                    return;
            }
        }
    }

    private bool IsAtEnd()
    {
        return _current == _source.Length;
    }

    private char Peek()
    {
        return _current >= _source.Length ? '\0' : _source[_current];
    }

    private char Advance()
    {
        return _source[_current++];
    }

    private char PeekNext()
    {
        return IsAtEnd() ? '\0' : _source[_current + 1];
    }

    private bool Match(char expected)
    {
        if (IsAtEnd()) return false;
        if (_source[_current] != expected) return false;
        _current++;
        return true;
    }

    private Token MakeToken(TokenType type)
    {
        return new Token(type, _source.Substring(_start, _current - _start), _line);
    }

    private Token ErrorToken(string message)
    {
        return new Token(TokenType.Error, message, _line);
    }

    private Token StringToken()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n') _line++;
            Advance();
        }

        if (IsAtEnd()) return ErrorToken("Unterminated string.");
        Advance();
        return MakeToken(TokenType.String);
    }

    private Token NumberToken()
    {
        while (char.IsDigit(Peek())) Advance();

        if (Peek() != '.' || !char.IsDigit(PeekNext())) return MakeToken(TokenType.Number);
        do
        {
            // Consume the ".".
            Advance();
        } while (char.IsDigit(Peek()));

        return MakeToken(TokenType.Number);
    }

    private static bool IsAlpha(char c)
    {
        return char.IsLetter(c) || c == '_';
    }

    private Token IdentifierToken()
    {
        while (IsAlpha(Peek()) || char.IsDigit(Peek())) Advance();

        return MakeToken(IdentifierType());
    }

    private TokenType IdentifierType()
    {
        switch (_source[_start])
        {
            case 'a': return CheckKeyWord(1, 2, "and", TokenType.And);
            case 'c': return CheckKeyWord(1, 4, "class", TokenType.Class);
            case 'e': return CheckKeyWord(1, 3, "else", TokenType.Else);
            case 'f':
                if (_current - _start > 1)
                    switch (_source[_start + 1])
                    {
                        case 'a':
                            return CheckKeyWord(2, 3, "false", TokenType.False);
                        case 'o':
                            return CheckKeyWord(2, 1, "for", TokenType.For);
                        case 'u':
                            return CheckKeyWord(2, 1, "fun", TokenType.Fun);
                    }

                break;
            case 'i': return CheckKeyWord(1, 1, "if", TokenType.If);
            case 'n': return CheckKeyWord(1, 2, "nil", TokenType.Nil);
            case 'o': return CheckKeyWord(1, 1, "or", TokenType.Or);
            case 'p': return CheckKeyWord(1, 4, "print", TokenType.Print);
            case 'r': return CheckKeyWord(1, 5, "return", TokenType.Return);
            case 's': return CheckKeyWord(1, 4, "super", TokenType.Super);
            case 't':
                if (_current - _start > 1)
                    switch (_source[_start + 1])
                    {
                        case 'h':
                            return CheckKeyWord(2, 2, "this", TokenType.This);
                        case 'r':
                            return CheckKeyWord(2, 2, "true", TokenType.True);
                    }

                break;
            case 'v': return CheckKeyWord(1, 2, "var", TokenType.Var);
            case 'w': return CheckKeyWord(1, 4, "while", TokenType.While);
        }

        return TokenType.Identifier;
    }

    private TokenType CheckKeyWord(int start, int length, string keyword, TokenType type)
    {
        // use whole keyword rather than "rest": "or" => "for", "ue" => "true"
        if (_current - _start == start + length &&
            _source.Substring(_start, start + length) == keyword)
        {
            return type;
        }

        return TokenType.Identifier;
    }
}