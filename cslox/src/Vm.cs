namespace cslox;

public enum InterpretResult
{
    Ok,
    CompileError,
    RuntimeError
}

public class Vm
{
    private const int StackMax = 256;
    private readonly Value[] _stack = new Value[StackMax];
    private Chunk? _chunk;
    private int _ip;
    private int _stackTop;
    private readonly Dictionary<string, Value> _globals = new();

    public InterpretResult Interpret(Chunk chunk)
    {
        _chunk = chunk;
        _ip = 0;
        return Run();
    }

    private void ResetStack()
    {
        _stackTop = 0;
    }

    private InterpretResult Run()
    {
        while (true)
        {
#if DEBUG
            Console.Write("          ");
            for (var i = 0; i < _stackTop; i++)
            {
                Console.Write("[");
                _stack[i].Print();
                // ValueArray.PrintValue(_stack[i]);
                Console.Write("]");
            }

            Console.WriteLine();
            _chunk!.DisassembleInstruction(_ip);
#endif
            var instruction = ReadByte();
            switch ((OpCode)instruction)
            {
                case OpCode.Constant:
                    var constant = ReadConstant();
                    Push(constant);
                    break;
                case OpCode.Nil:
                    Push(new Value());
                    break;
                case OpCode.True:
                    Push(new Value(true));
                    break;
                case OpCode.False:
                    Push(new Value(false));
                    break;
                case OpCode.Pop:
                    Pop();
                    break;
                case OpCode.GetGlobal:
                    var nameGetGlobal = ReadString();
                    if (!_globals.ContainsKey(nameGetGlobal))
                    {
                        RuntimeError("Undefined variable '", nameGetGlobal, "'.");
                        return InterpretResult.RuntimeError;
                    }

                    Push(_globals[nameGetGlobal]);
                    break;
                case OpCode.DefineGlobal:
                    var nameDefineGlobal = ReadString();
                    _globals[nameDefineGlobal] = Peek(0);
                    Pop();
                    break;
                case OpCode.SetGlobal:
                    var nameSetGlobal = ReadString();
                    if (!_globals.ContainsKey(nameSetGlobal))
                    {
                        RuntimeError("Undefined variable '", nameSetGlobal, "'.");
                        return InterpretResult.RuntimeError;
                    }

                    _globals[nameSetGlobal] = Peek(0);
                    break;
                case OpCode.Equal:
                    var b = Pop();
                    var a = Pop();
                    Push(new Value(a.Equal(b)));
                    break;
                case OpCode.Greater:
                case OpCode.Less:
                case OpCode.Add:
                case OpCode.Subtract:
                case OpCode.Multiply:
                case OpCode.Divide:
                    var result = BinaryOp((OpCode)instruction);
                    if (result != InterpretResult.Ok) return result;
                    break;
                case OpCode.Not:
                    Push(new Value(Pop().IsFalsey()));
                    break;
                case OpCode.Negate:
                    if (Peek(0).IsNumber())
                    {
                        RuntimeError("Operand must be a number.");
                        return InterpretResult.RuntimeError;
                    }

                    Push(new Value(-Pop().Number));
                    break;
                case OpCode.Print:
                    Pop().Print();
                    Console.WriteLine();
                    break;
                case OpCode.Return:
                    return InterpretResult.Ok;
                default:
                    Console.Error.WriteLine($"Unknown opcode {instruction}.");
                    return InterpretResult.CompileError;
            }
        }
    }

    private Value Peek(int distance)
    {
        return _stack[_stackTop - 1 - distance];
    }

    private byte ReadByte()
    {
        return _chunk!.ReadByte(_ip++);
    }

    private Value ReadConstant()
    {
        return _chunk!.ReadConstant(ReadByte());
    }

    private string ReadString()
    {
        return ReadConstant().String;
    }

    private void Push(Value value)
    {
        _stack[_stackTop++] = value;
    }

    private Value Pop()
    {
        return _stack[--_stackTop];
    }

    private InterpretResult BinaryOp(OpCode instruction)
    {
        if (instruction == OpCode.Add && Peek(0).IsString() && Peek(1).IsString())
        {
            var bString = Pop().String;
            var aString = Pop().String;
            Push(new Value(aString + bString));
            return InterpretResult.Ok;
        }

        if (!Peek(0).IsNumber() || !Peek(1).IsNumber())
        {
            RuntimeError("Operands must be numbers.");
            return InterpretResult.RuntimeError;
        }

        var b = Pop().Number;
        var a = Pop().Number;
        switch (instruction)
        {
            case OpCode.Greater:
                Push(new Value(a > b));
                break;
            case OpCode.Less:
                Push(new Value(a < b));
                break;
            case OpCode.Add:
                Push(new Value(a + b));
                break;
            case OpCode.Subtract:
                Push(new Value(a - b));
                break;
            case OpCode.Multiply:
                Push(new Value(a * b));
                break;
            case OpCode.Divide:
                Push(new Value(a / b));
                break;
            default:
                RuntimeError($"Wrong opcode {instruction}.");
                return InterpretResult.RuntimeError;
        }

        return InterpretResult.Ok;
    }

    private void RuntimeError(params string[] messages)
    {
        foreach (var msg in messages) Console.Error.Write(msg);
        Console.Error.WriteLine();
        var line = _chunk!.GetLineNumber(_ip);
        Console.Error.WriteLine($"[line {line}] in script.");
        ResetStack();
    }
}