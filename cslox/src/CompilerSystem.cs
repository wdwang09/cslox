namespace cslox;

public class CompilerSystem
{
    private readonly Dictionary<TokenType, ParseRule> _rules;
    private readonly Vm _vm = new();
    private Scanner? _scanner;
    private Parser? _parser;
    private SubCompiler? _current;
    private ClassCompiler? _currentClass;

    public CompilerSystem()
    {
        _rules = new Dictionary<TokenType, ParseRule>
        {
            [TokenType.LeftParen] = new(Grouping, Call, Precedence.Call),
            [TokenType.RightParen] = new(null, null, Precedence.None),
            [TokenType.LeftBrace] = new(null, null, Precedence.None),
            [TokenType.RightBrace] = new(null, null, Precedence.None),
            [TokenType.Comma] = new(null, null, Precedence.None),
            [TokenType.Dot] = new(null, Dot, Precedence.Call),
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
            [TokenType.Identifier] = new(Variable, null, Precedence.None),
            [TokenType.String] = new(String, null, Precedence.None),
            [TokenType.Number] = new(Number, null, Precedence.None),
            [TokenType.And] = new(null, And, Precedence.And),
            [TokenType.Class] = new(null, null, Precedence.None),
            [TokenType.Else] = new(null, null, Precedence.None),
            [TokenType.False] = new(Literal, null, Precedence.None),
            [TokenType.For] = new(null, null, Precedence.None),
            [TokenType.Fun] = new(null, null, Precedence.None),
            [TokenType.If] = new(null, null, Precedence.None),
            [TokenType.Nil] = new(Literal, null, Precedence.None),
            [TokenType.Or] = new(null, Or, Precedence.Or),
            [TokenType.Print] = new(null, null, Precedence.None),
            [TokenType.Assert] = new(null, null, Precedence.None),
            [TokenType.Return] = new(null, null, Precedence.None),
            [TokenType.Super] = new(Super, null, Precedence.None),
            [TokenType.This] = new(This, null, Precedence.None),
            [TokenType.True] = new(Literal, null, Precedence.None),
            [TokenType.Var] = new(null, null, Precedence.None),
            [TokenType.While] = new(null, null, Precedence.None),
            [TokenType.Error] = new(null, null, Precedence.None),
            [TokenType.Eof] = new(null, null, Precedence.None)
        };
    }

    internal InterpretResult CompileAndRun(string source)
    {
#if DEBUG
        var line = new string('=', 45);
        Console.WriteLine(line);
#endif
        var function = Compile(source);
#if DEBUG
        Console.WriteLine(line);
#endif
        return function is null ? InterpretResult.CompileError : _vm.Interpret(function);
    }

    private ObjFunction? Compile(string source)
    {
        _scanner = new Scanner(source);
        _parser = new Parser();
        _current = new SubCompiler(null, FunctionType.Script);
        Advance();
        while (!Match(TokenType.Eof))
        {
            Declaration();
        }

        var function = EndCompiler();
        return _parser.HadError ? null : function;
    }

    private bool Match(TokenType type)
    {
        if (!Check(type)) return false;
        Advance();
        return true;
    }

    private bool Check(TokenType type)
    {
        return _parser!.Current!.Value.Type == type;
    }

    private void Declaration()
    {
        if (Match(TokenType.Class))
        {
            ClassDeclaration();
        }
        else if (Match(TokenType.Fun))
        {
            FunDeclaration();
        }
        else if (Match(TokenType.Var))
        {
            VarDeclaration();
        }
        else
        {
            Statement();
        }

        if (_parser!.PanicMode)
        {
            Synchronize();
        }
    }

    private void ClassDeclaration()
    {
        Consume(TokenType.Identifier, "Expect class name.");
        var className = _parser!.Previous!.Value;
        var nameConstant = IdentifierConstant(_parser!.Previous!.Value);
        DeclareVariable();

        EmitBytes(OpCode.Class, nameConstant);
        DefineVariable(nameConstant);

        var classCompiler = new ClassCompiler(_currentClass);
        _currentClass = classCompiler;

        if (Match(TokenType.Less))
        {
            Consume(TokenType.Identifier, "Expect superclass name.");
            Variable(false);

            if (className.Lexeme == _parser!.Previous!.Value.Lexeme)
            {
                _parser!.Error("A class can't inherit from itself.");
            }

            BeginScope();
            AddLocal("super");
            DefineVariable(0);

            NamedVariable(className, false);
            EmitByte(OpCode.Inherit);
            classCompiler.HasSuperclass = true;
        }

        NamedVariable(className, false);
        Consume(TokenType.LeftBrace, "Expect '{' before class body.");
        while (!Check(TokenType.RightBrace) && !Check(TokenType.Eof))
        {
            Method();
        }

        Consume(TokenType.RightBrace, "Expect '}' after class body.");
        EmitByte(OpCode.Pop);

        if (classCompiler.HasSuperclass)
        {
            EndScope();
        }

        _currentClass = _currentClass.Enclosing;
    }

    private void FunDeclaration()
    {
        var global = ParseVariable("Expect function name.");
        MarkInitialized();
        Function(FunctionType.Function);
        DefineVariable(global);
    }

    private void VarDeclaration()
    {
        var global = ParseVariable("Expect variable name.");

        if (Match(TokenType.Equal))
        {
            Expression();
        }
        else
        {
            EmitByte(OpCode.Nil);
        }

        Consume(TokenType.Semicolon, "Expect ';' after variable declaration.");
        DefineVariable(global);
    }

    private void Statement()
    {
        if (Match(TokenType.Print))
        {
            PrintStatement();
        }
        else if (Match(TokenType.Assert))
        {
            AssertStatement();
        }
        else if (Match(TokenType.If))
        {
            IfStatement();
        }
        else if (Match(TokenType.Return))
        {
            ReturnStatement();
        }
        else if (Match(TokenType.While))
        {
            WhileStatement();
        }
        else if (Match(TokenType.For))
        {
            ForStatement();
        }
        else if (Match(TokenType.LeftBrace))
        {
            BeginScope();
            Block();
            EndScope();
        }
        else
        {
            ExpressionStatement();
        }
    }

    private void PrintStatement()
    {
        Expression();
        Consume(TokenType.Semicolon, "Expect ';' after value.");
        EmitByte(OpCode.Print);
    }

    private void AssertStatement()
    {
        Expression();
        Consume(TokenType.Semicolon, "Expect ';' after value.");
        EmitByte(OpCode.Assert);
    }

    private void IfStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'if'.");
        Expression();
        Consume(TokenType.RightParen, "Expect ')' after condition.");

        var thenJump = EmitJump(OpCode.JumpIfFalse);
        EmitByte(OpCode.Pop);
        Statement();
        var elseJump = EmitJump(OpCode.Jump);
        PatchJump(thenJump);
        EmitByte(OpCode.Pop);

        if (Match(TokenType.Else))
        {
            Statement();
        }

        PatchJump(elseJump);
    }

    private void ReturnStatement()
    {
        if (_current!.Type == FunctionType.Script)
        {
            _parser!.Error("Can't return from top-level code.");
        }

        if (Match(TokenType.Semicolon))
        {
            EmitReturn();
        }
        else
        {
            if (_current!.Type == FunctionType.Initializer)
            {
                _parser!.Error("Can't return a value from an initializer.");
            }

            Expression();
            Consume(TokenType.Semicolon, "Expect ';' after return value.");
            EmitByte(OpCode.Return);
        }
    }

    private void WhileStatement()
    {
        var loopStart = CurrentChunk().Count;
        Consume(TokenType.LeftParen, "Expect '(' after 'while'.");
        Expression();
        Consume(TokenType.RightParen, "Expect ')' after condition.");

        var exitJump = EmitJump(OpCode.JumpIfFalse);
        EmitByte(OpCode.Pop);
        Statement();
        EmitLoop(loopStart);

        PatchJump(exitJump);
        EmitByte(OpCode.Pop);
    }

    private void ForStatement()
    {
        BeginScope();
        Consume(TokenType.LeftParen, "Expect '(' after 'for'.");
        if (Match(TokenType.Semicolon))
        {
            // No initializer.
        }
        else if (Match(TokenType.Var))
        {
            VarDeclaration();
        }
        else
        {
            ExpressionStatement();
        }

        var loopStart = CurrentChunk().Count;
        var exitJump = -1;
        if (!Match(TokenType.Semicolon))
        {
            Expression();
            Consume(TokenType.Semicolon, "Expect ';' after loop condition.");

            exitJump = EmitJump(OpCode.JumpIfFalse);
            EmitByte(OpCode.Pop);
        }

        if (!Match(TokenType.RightParen))
        {
            var bodyJump = EmitJump(OpCode.Jump);
            var incrementStart = CurrentChunk().Count;
            Expression();
            EmitByte(OpCode.Pop);
            Consume(TokenType.RightParen, "Expect ')' after for clauses.");

            EmitLoop(loopStart);
            loopStart = incrementStart;
            PatchJump(bodyJump);
        }

        Statement();
        EmitLoop(loopStart);

        if (exitJump != -1)
        {
            PatchJump(exitJump);
            EmitByte(OpCode.Pop);
        }

        EndScope();
    }

    private void BeginScope()
    {
        _current!.ScopeDepth++;
    }

    private void EndScope()
    {
        _current!.ScopeDepth--;
        while (_current!.LocalCount > 0 &&
               _current!.Locals[_current!.LocalCount - 1].Depth > _current!.ScopeDepth)
        {
            var isCaptured = _current!.Locals[_current!.LocalCount - 1].IsCaptured;
            EmitByte(isCaptured ? OpCode.CloseUpvalue : OpCode.Pop);
            _current!.LocalCount--;
        }
    }

    private void Block()
    {
        while (!Check(TokenType.RightBrace) && !Check(TokenType.Eof))
        {
            Declaration();
        }

        Consume(TokenType.RightBrace, "Expect '}' after block.");
    }

    private void ExpressionStatement()
    {
        Expression();
        Consume(TokenType.Semicolon, "Expect ';' after expression.");
        EmitByte(OpCode.Pop);
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

        var canAssign = (precedence <= Precedence.Assignment);
        prefixRule(canAssign);

        while (precedence <= _rules[_parser!.Current!.Value.Type].Precedence)
        {
            Advance();
            var infixRule = _rules[_parser!.Previous!.Value.Type].Infix;
            infixRule!(canAssign);
        }

        if (canAssign && Match(TokenType.Equal))
        {
            _parser.Error("Invalid assignment target.");
        }
    }

    private byte ParseVariable(string errorMessage)
    {
        Consume(TokenType.Identifier, errorMessage);

        DeclareVariable();
        if (_current!.ScopeDepth > 0) return 0;

        return IdentifierConstant(_parser!.Previous!.Value);
    }

    private void DeclareVariable()
    {
        if (_current!.ScopeDepth == 0) return;
        var name = _parser!.Previous!.Value.Lexeme;
        for (var i = _current!.LocalCount - 1; i >= 0; i--)
        {
            ref var local = ref _current!.Locals[i];
            if (local.Depth != -1 && local.Depth < _current!.ScopeDepth)
            {
                break;
            }

            if (name == local.Name)
            {
                _parser!.Error("Already a variable with this name in this scope.");
            }
        }

        AddLocal(name);
    }

    private void AddLocal(string name)
    {
        if (_current!.LocalCount == SubCompiler.LocalMax)
        {
            _parser!.Error("Too many local variables in function.");
            return;
        }

        ref var local = ref _current!.Locals[_current!.LocalCount++];
        local.Name = name;
        local.Depth = -1;
        local.IsCaptured = false;
    }

    private void DefineVariable(byte global)
    {
        if (_current!.ScopeDepth > 0)
        {
            MarkInitialized();
            return;
        }

        EmitBytes(OpCode.DefineGlobal, global);
    }

    private void MarkInitialized()
    {
        if (_current!.ScopeDepth == 0) return;
        _current!.Locals[_current!.LocalCount - 1].Depth = _current!.ScopeDepth;
    }

    private void Function(FunctionType type)
    {
        var possibleFunctionName = "";
        if (type != FunctionType.Script)
        {
            possibleFunctionName = _parser!.Previous!.Value.Lexeme;
        }

        var subCompiler = new SubCompiler(_current, type, possibleFunctionName);
        _current = subCompiler;
        BeginScope();
        Consume(TokenType.LeftParen, "Expect '(' after function name.");
        if (!Check(TokenType.RightParen))
        {
            do
            {
                _current.Function.Arity++;
                if (_current.Function.Arity > 255)
                {
                    _parser!.ErrorAtCurrent("Can't have more than 255 parameters.");
                }

                var constant = ParseVariable("Expect parameter name.");
                DefineVariable(constant);
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParen, "Expect ')' after parameters.");
        Consume(TokenType.LeftBrace, "Expect '{' before function body.");
        Block();
        var function = EndCompiler();
        EmitBytes(OpCode.Closure, MakeConstant(new Value(function)));

        for (var i = 0; i < function.UpvalueCount; ++i)
        {
            EmitByte((byte)(subCompiler.Upvalues[i].IsLocal ? 1 : 0));
            EmitByte(subCompiler.Upvalues[i].Index);
        }
    }

    private void Method()
    {
        Consume(TokenType.Identifier, "Expect method name.");
        var constant = IdentifierConstant(_parser!.Previous!.Value);

        var type = FunctionType.Method;
        if (_parser!.Previous!.Value.Lexeme == ObjClass.InitString)
        {
            type = FunctionType.Initializer;
        }

        Function(type);
        EmitBytes(OpCode.Method, constant);
    }

    private int ResolveLocal(SubCompiler compiler, string name)
    {
        for (var i = compiler.LocalCount - 1; i >= 0; i--)
        {
            ref var local = ref compiler.Locals[i];
            if (name != local.Name) continue;
            if (local.Depth == -1)
            {
                _parser!.Error("Can't read local variable in its own initializer.");
            }

            return i;
        }

        return -1;
    }

    private int ResolveUpvalue(SubCompiler compiler, string name)
    {
        if (compiler.Enclosing is null) return -1;
        var local = ResolveLocal(compiler.Enclosing, name);
        if (local != -1)
        {
            compiler.Enclosing.Locals[local].IsCaptured = true;
            return AddUpvalue(compiler, (byte)local, true);
        }

        var upvalue = ResolveUpvalue(compiler.Enclosing, name);
        if (upvalue != -1)
        {
            return AddUpvalue(compiler, (byte)upvalue, false);
        }

        return -1;
    }

    private int AddUpvalue(SubCompiler compiler, byte index, bool isLocal)
    {
        var upvalueCount = compiler.Function.UpvalueCount;
        for (var i = 0; i < upvalueCount; ++i)
        {
            ref var upvalue = ref compiler.Upvalues[i];
            if (upvalue.Index == index && upvalue.IsLocal == isLocal)
            {
                return i;
            }
        }

        if (upvalueCount == SubCompiler.UpvalueMax)
        {
            _parser!.Error("Too many closure variables in function.");
            return 0;
        }

        compiler.Upvalues[upvalueCount].IsLocal = isLocal;
        compiler.Upvalues[upvalueCount].Index = index;
        return compiler.Function.UpvalueCount++;
    }

    private byte IdentifierConstant(Token name)
    {
        return MakeConstant(new Value(name.Lexeme));
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
        CurrentChunk().WriteChunk(@byte, _parser!.Previous!.Value.Line);
    }

    private void EmitByte(OpCode @byte)
    {
        CurrentChunk().WriteChunk((byte)@byte, _parser!.Previous!.Value.Line);
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

    private void EmitLoop(int loopStart)
    {
        EmitByte(OpCode.Loop);
        var offset = CurrentChunk().Count - loopStart + 2;
        if (offset > ushort.MaxValue)
        {
            _parser!.Error("Loop body is too large.");
        }

        EmitByte((byte)((offset >> 8) & 0xff));
        EmitByte((byte)(offset & 0xff));
    }

    private int EmitJump(OpCode instruction)
    {
        EmitByte(instruction);
        EmitByte(byte.MaxValue);
        EmitByte(byte.MaxValue);
        return CurrentChunk().Count - 2;
    }

    private void PatchJump(int offset)
    {
        if (!CurrentChunk().PatchJump(offset))
        {
            _parser!.Error("Too much code to jump over.");
        }
    }

    private ObjFunction EndCompiler()
    {
        EmitReturn();
        var function = _current!.Function;
#if DEBUG
        if (!_parser!.HadError)
        {
            var name = function.Name.Length > 0 ? function.Name : "<script>";
            CurrentChunk().DisassembleChunk(name);
        }
#endif
        _current = _current.Enclosing;
        return function;
    }

    private void EmitReturn()
    {
        if (_current!.Type == FunctionType.Initializer)
        {
            EmitBytes(OpCode.GetLocal, (byte)0);
        }
        else
        {
            EmitByte(OpCode.Nil);
        }

        EmitByte(OpCode.Return);
    }

    private void EmitConstant(Value value)
    {
        EmitBytes(OpCode.Constant, MakeConstant(value));
    }

    private byte MakeConstant(Value value)
    {
        var constant = CurrentChunk().AddConstant(value);
        if (constant > byte.MaxValue)
        {
            _parser!.Error("Too many constants in one chunk.");
            return 0;
        }

        return (byte)constant;
    }

    private void Number(bool canAssign)
    {
        var value = double.Parse(_parser!.Previous!.Value.Lexeme);
        EmitConstant(new Value(value));
    }

    private void Grouping(bool canAssign)
    {
        Expression();
        Consume(TokenType.RightParen, "Expect ')' after expression.");
    }

    private void Unary(bool canAssign)
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

    private void Binary(bool canAssign)
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

    private void Call(bool canAssign)
    {
        var argCount = ArgumentList();
        EmitBytes(OpCode.Call, argCount);
    }

    private void Dot(bool canAssign)
    {
        Consume(TokenType.Identifier, "Expect property name after '.'.");
        var name = IdentifierConstant(_parser!.Previous!.Value);

        if (canAssign && Match(TokenType.Equal))
        {
            Expression();
            EmitBytes(OpCode.SetProperty, name);
        }
        else if (Match(TokenType.LeftParen))
        {
            var argCount = ArgumentList();
            EmitBytes(OpCode.Invoke, name);
            EmitByte(argCount);
        }
        else
        {
            EmitBytes(OpCode.GetProperty, name);
        }
    }

    private byte ArgumentList()
    {
        byte argCount = 0;
        if (!Check(TokenType.RightParen))
        {
            do
            {
                Expression();
                if (argCount == 255)
                {
                    _parser!.Error("Can't have more than 255 arguments.");
                }

                argCount++;
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParen, "Expect ')' after arguments.");
        return argCount;
    }

    private void Literal(bool canAssign)
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

    private void String(bool canAssign)
    {
        var str = _parser!.Previous!.Value.Lexeme;
        EmitConstant(new Value(str.Substring(1, str.Length - 2)));
    }

    private void Variable(bool canAssign)
    {
        NamedVariable(_parser!.Previous!.Value, canAssign);
    }

    private void NamedVariable(Token name, bool canAssign)
    {
        OpCode getOp, setOp;
        var arg = ResolveLocal(_current!, name.Lexeme);
        if (arg != -1)
        {
            getOp = OpCode.GetLocal;
            setOp = OpCode.SetLocal;
        }
        else if ((arg = ResolveUpvalue(_current!, name.Lexeme)) != -1)
        {
            getOp = OpCode.GetUpvalue;
            setOp = OpCode.SetUpvalue;
        }
        else
        {
            arg = IdentifierConstant(name);
            getOp = OpCode.GetGlobal;
            setOp = OpCode.SetGlobal;
        }

        if (canAssign && Match(TokenType.Equal))
        {
            Expression();
            EmitBytes(setOp, (byte)arg);
        }
        else
        {
            EmitBytes(getOp, (byte)arg);
        }
    }

    private void This(bool canAssign)
    {
        if (_currentClass is null)
        {
            _parser!.Error("Can't use 'this' outside of a class.");
            return;
        }

        Variable(false);
    }

    private void Super(bool canAssign)
    {
        if (_currentClass is null)
        {
            _parser!.Error("Can't use 'super' outside of a class.");
        }
        else if (!_currentClass.HasSuperclass)
        {
            _parser!.Error("Can't use 'super' in a class with no superclass.");
        }

        Consume(TokenType.Dot, "Expect '.' after 'super'.");
        Consume(TokenType.Identifier, "Expect superclass method name.");
        var name = IdentifierConstant(_parser!.Previous!.Value);

        NamedVariable(new Token(TokenType.Synthetic, "this", 0), false);
        if (Match(TokenType.LeftParen))
        {
            var argCount = ArgumentList();
            NamedVariable(new Token(TokenType.Synthetic, "super", 0), false);
            EmitBytes(OpCode.SuperInvoke, name);
            EmitByte(argCount);
        }
        else
        {
            NamedVariable(new Token(TokenType.Synthetic, "super", 0), false);
            EmitBytes(OpCode.GetSuper, name);
        }
    }

    private void And(bool canAssign)
    {
        var endJump = EmitJump(OpCode.JumpIfFalse);
        EmitByte(OpCode.Pop);
        ParsePrecedence(Precedence.And);
        PatchJump(endJump);
    }

    private void Or(bool canAssign)
    {
        var elseJump = EmitJump(OpCode.JumpIfFalse);
        var endJump = EmitJump(OpCode.Jump);
        PatchJump(elseJump);
        EmitByte(OpCode.Pop);
        ParsePrecedence(Precedence.Or);
        PatchJump(endJump);
    }

    private void Synchronize()
    {
        _parser!.PanicMode = false;
        while (_parser!.Current!.Value.Type != TokenType.Eof)
        {
            if (_parser!.Previous!.Value.Type == TokenType.Semicolon) return;
            switch (_parser!.Current!.Value.Type)
            {
                case TokenType.Class:
                case TokenType.Fun:
                case TokenType.Var:
                case TokenType.For:
                case TokenType.If:
                case TokenType.While:
                case TokenType.Print:
                case TokenType.Assert:
                case TokenType.Return:
                    return;
            }

            Advance();
        }
    }

    private Chunk CurrentChunk()
    {
        return _current!.Function.Chunk;
    }
}