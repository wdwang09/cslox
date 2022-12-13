namespace cslox;

internal struct CallFrame
{
    internal ObjFunction Function;
    internal int Ip;
    internal int Slots;

    internal byte ReadByte()
    {
        return Function.Chunk.ReadByte(Ip++);
    }

    internal Value ReadConstant()
    {
        return Function.Chunk.ReadConstant(ReadByte());
    }

    internal ushort ReadShort()
    {
        Ip += 2;
        var readByte = Function.Chunk.ReadByte;
        return (ushort)((readByte(Ip - 2) << 8) | readByte(Ip - 1));
    }

    internal string ReadString()
    {
        return ReadConstant().String;
    }
}

public enum InterpretResult
{
    Ok,
    CompileError,
    RuntimeError
}

internal class Vm
{
    private const int FramesMax = 64;
    private readonly CallFrame[] _frames = new CallFrame[FramesMax];
    private int _frameCount;
    private const int StackMax = 256 * FramesMax;
    private readonly Value[] _stack = new Value[StackMax];
    private int _stackTop;
    private readonly Dictionary<string, Value> _globals = new();

    internal Vm()
    {
        DefineNative("clock", ObjNative.ClockNative);
    }

    public InterpretResult Interpret(ObjFunction function)
    {
        Push(new Value(function));
        Call(function, 0);
        return Run();
    }

    private void ResetStack()
    {
        _stackTop = 0;
        _frameCount = 0;
    }

    private InterpretResult Run()
    {
        ref var frame = ref _frames[_frameCount - 1];
        while (true)
        {
#if DEBUG
            Console.Write("          ");
            for (var i = 0; i < _stackTop; i++)
            {
                Console.Write("[");
                _stack[i].Print();
                Console.Write("]");
            }

            Console.WriteLine();
            frame.Function.Chunk.DisassembleInstruction(frame.Ip);
#endif
            var instruction = frame.ReadByte();
            switch ((OpCode)instruction)
            {
                case OpCode.Constant:
                {
                    var constant = frame.ReadConstant();
                    Push(constant);
                    break;
                }
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
                case OpCode.GetLocal:
                {
                    var slot = frame.ReadByte();
                    Push(_stack[slot + frame.Slots]);
                    break;
                }
                case OpCode.SetLocal:
                {
                    var slot = frame.ReadByte();
                    _stack[slot + frame.Slots] = Peek(0);
                    break;
                }
                case OpCode.GetGlobal:
                {
                    var name = frame.ReadString();
                    if (!_globals.ContainsKey(name))
                    {
                        RuntimeError("Undefined variable when getting: '", name, "'.");
                        return InterpretResult.RuntimeError;
                    }

                    Push(_globals[name]);
                    break;
                }
                case OpCode.DefineGlobal:
                {
                    var name = frame.ReadString();
                    _globals[name] = Peek(0);
                    Pop();
                    break;
                }
                case OpCode.SetGlobal:
                {
                    var name = frame.ReadString();
                    if (!_globals.ContainsKey(name))
                    {
                        RuntimeError("Undefined variable when setting: '", name, "'.");
                        return InterpretResult.RuntimeError;
                    }

                    _globals[name] = Peek(0);
                    break;
                }
                case OpCode.Equal:
                {
                    var b = Pop();
                    var a = Pop();
                    Push(new Value(a.Equal(b)));
                    break;
                }
                case OpCode.Greater:
                case OpCode.Less:
                case OpCode.Add:
                case OpCode.Subtract:
                case OpCode.Multiply:
                case OpCode.Divide:
                {
                    var result = BinaryOp((OpCode)instruction);
                    if (result != InterpretResult.Ok) return result;
                    break;
                }
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
                case OpCode.Jump:
                {
                    var offset = frame.ReadShort();
                    frame.Ip += offset;
                    break;
                }
                case OpCode.JumpIfFalse:
                {
                    var offset = frame.ReadShort();
                    if (Peek(0).IsFalsey())
                    {
                        frame.Ip += offset;
                    }

                    break;
                }
                case OpCode.Loop:
                {
                    var offset = frame.ReadShort();
                    frame.Ip -= offset;
                    break;
                }
                case OpCode.Call:
                {
                    var argCount = frame.ReadByte();
                    if (!CallValue(Peek(argCount), argCount))
                    {
                        return InterpretResult.RuntimeError;
                    }

                    frame = ref _frames[_frameCount - 1];
                    break;
                }
                case OpCode.Return:
                {
                    var result = Pop();
                    _frameCount--;
                    if (_frameCount == 0)
                    {
                        Pop();
                        return InterpretResult.Ok;
                    }

                    _stackTop = frame.Slots;
                    Push(result);
                    frame = ref _frames[_frameCount - 1];
                    break;
                }
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

    private bool CallValue(Value callee, int argCount)
    {
        if (callee.IsObj())
        {
            switch (callee.Obj.Type)
            {
                case ObjType.Function:
                    return Call((ObjFunction)callee.Obj, argCount);
                case ObjType.Native:
                {
                    var native = ((ObjNative)callee.Obj).Function;
                    var argList = new List<Value>();
                    for (var i = 0; i < argCount; ++i)
                    {
                        argList.Add(_stack[_stackTop - i - 1]);
                    }

                    var result = native(argCount, argList);
                    _stackTop -= argCount + 1;
                    Push(result);
                    return true;
                }
                default:
                    Console.Error.WriteLine("Unsupported.");
                    break;
            }
        }

        RuntimeError("Can only call functions and classes.");
        return false;
    }

    private bool Call(ObjFunction function, int argCount)
    {
        if (argCount != function.Arity)
        {
            RuntimeError($"Expected {function.Arity} arguments but got {argCount}.");
            return false;
        }

        if (_frameCount == FramesMax)
        {
            RuntimeError("Stack overflow.");
            return false;
        }

        ref var frame = ref _frames[_frameCount++];
        frame.Function = function;
        frame.Ip = 0;
        frame.Slots = _stackTop - argCount - 1;
        return true;
    }

    private void RuntimeError(params string[] messages)
    {
        foreach (var msg in messages) Console.Error.Write(msg);
        Console.Error.WriteLine();
        for (var i = _frameCount - 1; i >= 0; i--)
        {
            ref var frame = ref _frames[i];
            ref var function = ref frame.Function;
            var instruction = frame.Ip - 1;
            Console.Error.Write($"[Line {function.Chunk.GetLineNumber(instruction)}] in ");
            Console.Error.WriteLine(function.Name == "" ? "script" : $"{function.Name}()");
        }

        ResetStack();
    }

    private void DefineNative(string name, NativeFn function)
    {
        Push(new Value(name));
        Push(new Value(new ObjNative(function)));
        _globals[_stack[0].String] = _stack[1];
        Pop();
        Pop();
    }
}