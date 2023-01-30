namespace cslox;

internal struct CallFrame
{
    internal ObjClosure Closure;
    internal int Ip;
    internal int Slots;

    internal byte ReadByte()
    {
        return Closure.Function.Chunk.ReadByte(Ip++);
    }

    internal Value ReadConstant()
    {
        return Closure.Function.Chunk.ReadConstant(ReadByte());
    }

    internal ushort ReadShort()
    {
        Ip += 2;
        var readByte = Closure.Function.Chunk.ReadByte;
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
    private ObjUpvalue? _openUpvalues;

    internal Vm()
    {
        DefineNative("clock", ObjNative.ClockNative);
    }

    public InterpretResult Interpret(ObjFunction function)
    {
        Push(new Value(function));
        var closure = new ObjClosure(function);
        Pop();
        Push(new Value(closure));
        Call(closure, 0);
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
            frame.Closure.Function.Chunk.DisassembleInstruction(frame.Ip);
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
                case OpCode.GetUpvalue:
                {
                    var slot = frame.ReadByte();
                    var idx = frame.Closure.Upvalues[slot].LocationIndex;
                    Push(idx >= 0 ? _stack[idx] : frame.Closure.Upvalues[slot].Closed);
                    break;
                }
                case OpCode.SetUpvalue:
                {
                    var slot = frame.ReadByte();
                    var idx = frame.Closure.Upvalues[slot].LocationIndex;
                    if (idx >= 0)
                    {
                        _stack[idx] = Peek(0);
                    }
                    else
                    {
                        frame.Closure.Upvalues[slot].Closed = Peek(0);
                    }

                    break;
                }
                case OpCode.GetProperty:
                {
                    if (!Peek(0).IsObjType(ObjType.Instance))
                    {
                        RuntimeError("Only instances have properties.");
                        return InterpretResult.RuntimeError;
                    }

                    var instance = (ObjInstance)Peek(0).Obj;
                    var name = frame.ReadString();

                    if (instance.Fields.ContainsKey(name))
                    {
                        var value = instance.Fields[name];
                        Pop();
                        Push(value);
                        break;
                    }

                    if (!BindMethod(instance.Class, name))
                    {
                        return InterpretResult.RuntimeError;
                    }

                    break;
                }
                case OpCode.SetProperty:
                {
                    if (!Peek(1).IsObjType(ObjType.Instance))
                    {
                        RuntimeError("Only instances have fields.");
                        return InterpretResult.RuntimeError;
                    }

                    var instance = (ObjInstance)Peek(1).Obj;
                    instance.Fields[frame.ReadString()] = Peek(0);
                    var value = Pop();
                    Pop();
                    Push(value);
                    break;
                }
                case OpCode.GetSuper:
                {
                    var name = frame.ReadString();
                    var superclass = (ObjClass)Pop().Obj;

                    if (!BindMethod(superclass, name))
                    {
                        return InterpretResult.RuntimeError;
                    }

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
                case OpCode.Assert:
                {
                    var expr = Pop();
                    if (expr.IsBool())
                    {
                        if (expr.IsTrue())
                        {
                            break;
                        }

                        RuntimeError("Assertion with False.");
                        return InterpretResult.RuntimeError;
                    }

                    RuntimeError("Currently assertion only supports boolean expression.");
                    return InterpretResult.RuntimeError;
                }
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
                case OpCode.Invoke:
                {
                    var method = frame.ReadString();
                    var argCount = frame.ReadByte();
                    if (!Invoke(method, argCount))
                    {
                        return InterpretResult.RuntimeError;
                    }

                    frame = ref _frames[_frameCount - 1];
                    break;
                }
                case OpCode.SuperInvoke:
                {
                    var method = frame.ReadString();
                    var argCount = frame.ReadByte();
                    var superclass = (ObjClass)Pop().Obj;
                    if (!InvokeFromClass(superclass, method, argCount))
                    {
                        return InterpretResult.RuntimeError;
                    }

                    frame = ref _frames[_frameCount - 1];
                    break;
                }
                case OpCode.Closure:
                {
                    var function = (ObjFunction)frame.ReadConstant().Obj;
                    var closure = new ObjClosure(function);
                    Push(new Value(closure));
                    for (var i = 0; i < closure.UpvalueCount; ++i)
                    {
                        var isLocal = frame.ReadByte();
                        var index = frame.ReadByte();
                        if (isLocal > 0)
                        {
                            closure.Upvalues[i] = CaptureUpvalue(frame.Slots + index);
                        }
                        else
                        {
                            closure.Upvalues[i] = frame.Closure.Upvalues[index];
                        }
                    }

                    break;
                }
                case OpCode.CloseUpvalue:
                {
                    CloseUpvalues(_stackTop - 1);
                    Pop();
                    break;
                }
                case OpCode.Return:
                {
                    var result = Pop();
                    CloseUpvalues(frame.Slots);
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
                case OpCode.Class:
                    Push(new Value(new ObjClass(frame.ReadString())));
                    break;
                case OpCode.Inherit:
                {
                    var superclass = Peek(1);
                    if (!superclass.IsObjType(ObjType.Class))
                    {
                        RuntimeError("Superclass must be a class.");
                        return InterpretResult.RuntimeError;
                    }

                    var subclass = (ObjClass)Peek(0).Obj;
                    foreach (var method in ((ObjClass)superclass.Obj).Methods)
                    {
                        subclass.Methods.Add(method.Key, method.Value);
                    }

                    Pop();
                    break;
                }
                case OpCode.Method:
                    DefineMethod(frame.ReadString());
                    break;
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
                case ObjType.BoundMethod:
                {
                    var bound = (ObjBoundMethod)callee.Obj;
                    _stack[_stackTop - argCount - 1] = bound.Receiver;
                    return Call(bound.Method, argCount);
                }
                case ObjType.Class:
                {
                    var objClass = (ObjClass)callee.Obj;
                    _stack[_stackTop - argCount - 1] = new Value(new ObjInstance(objClass));
                    if (objClass.Methods.ContainsKey(ObjClass.InitString))
                    {
                        var initializer = objClass.Methods[ObjClass.InitString];
                        return Call((ObjClosure)initializer.Obj, argCount);
                    }
                    else if (argCount != 0)
                    {
                        RuntimeError($"Expected 0 arguments but got {argCount}.");
                        return false;
                    }

                    return true;
                }
                case ObjType.Closure:
                    return Call((ObjClosure)callee.Obj, argCount);
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

    private bool Invoke(string name, int argCount)
    {
        var receiver = Peek(argCount);

        if (!receiver.IsObjType(ObjType.Instance))
        {
            RuntimeError("Only instances have methods.");
            return false;
        }

        var instance = (ObjInstance)receiver.Obj;

        if (instance.Fields.TryGetValue(name, out var value))
        {
            _stack[_stackTop - argCount - 1] = value;
            return CallValue(value, argCount);
        }

        return InvokeFromClass(instance.Class, name, argCount);
    }

    private bool InvokeFromClass(ObjClass @class, string name, int argCount)
    {
        if (!@class.Methods.ContainsKey(name))
        {
            RuntimeError($"Undefined property '{name}'.");
            return false;
        }

        return Call((ObjClosure)@class.Methods[name].Obj, argCount);
    }

    private bool BindMethod(ObjClass @class, string name)
    {
        if (!@class.Methods.ContainsKey(name))
        {
            RuntimeError($"Undefined property '{name}'.");
            return false;
        }

        var method = @class.Methods[name];
        var bound = new ObjBoundMethod(Peek(0), (ObjClosure)method.Obj);
        Pop();
        Push(new Value(bound));
        return true;
    }

    private bool Call(ObjClosure closure, int argCount)
    {
        if (argCount != closure.Function.Arity)
        {
            RuntimeError($"Expected {closure.Function.Arity} arguments " +
                         $"but got {argCount}.");
            return false;
        }

        if (_frameCount == FramesMax)
        {
            RuntimeError("Stack overflow.");
            return false;
        }

        ref var frame = ref _frames[_frameCount++];
        frame.Closure = closure;
        frame.Ip = 0;
        frame.Slots = _stackTop - argCount - 1;
        return true;
    }

    private ObjUpvalue CaptureUpvalue(int local)
    {
        ObjUpvalue? prevUpvalue = null;
        var upvalue = _openUpvalues;

        while (upvalue is not null && upvalue.LocationIndex > local)
        {
            prevUpvalue = upvalue;
            upvalue = upvalue.Next;
        }

        if (upvalue is not null && upvalue.LocationIndex == local)
        {
            return upvalue;
        }

        var createdUpvalue = new ObjUpvalue(local, upvalue);
        if (prevUpvalue is null)
        {
            _openUpvalues = createdUpvalue;
        }
        else
        {
            prevUpvalue.Next = createdUpvalue;
        }

        return createdUpvalue;
    }

    private void CloseUpvalues(int last)
    {
        while (_openUpvalues is not null && _openUpvalues.LocationIndex >= last)
        {
            _openUpvalues.Closed = _stack[_openUpvalues.LocationIndex];
            _openUpvalues.LocationIndex = -1;
            _openUpvalues = _openUpvalues.Next;
        }
    }

    private void DefineMethod(string name)
    {
        var method = Peek(0);
        var objClass = (ObjClass)Peek(1).Obj;
        objClass.Methods[name] = method;
        Pop();
    }

    private void RuntimeError(params string[] messages)
    {
        foreach (var msg in messages) Console.Error.Write(msg);
        Console.Error.WriteLine();
        for (var i = _frameCount - 1; i >= 0; i--)
        {
            ref var frame = ref _frames[i];
            var instruction = frame.Ip - 1;
            Console.Error.Write($"[Line {frame.Closure.Function.Chunk.GetLineNumber(instruction)}] in ");
            var output = frame.Closure.Function.Name == "" ? "script" : frame.Closure.Function.Name;
            Console.Error.WriteLine(output);
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