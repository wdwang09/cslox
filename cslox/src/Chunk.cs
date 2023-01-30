namespace cslox;

internal enum OpCode : byte
{
    Constant,
    Nil,
    True,
    False,
    Pop,
    GetLocal,
    SetLocal,
    GetGlobal,
    DefineGlobal,
    SetGlobal,
    GetUpvalue,
    SetUpvalue,
    GetProperty,
    SetProperty,
    GetSuper,
    Equal,
    Greater,
    Less,
    Add,
    Subtract,
    Multiply,
    Divide,
    Not,
    Negate,
    Print,
    Jump,
    JumpIfFalse,
    Loop,
    Call,
    Invoke,
    SuperInvoke,
    Closure,
    CloseUpvalue,
    Return,
    Class,
    Inherit,
    Method,
    Assert
}

public class Chunk
{
    private readonly List<byte> _code = new();
    private readonly ValueArray _constants = new();
    private readonly List<int> _lines = new();
    internal int Count => _code.Count;

    internal void WriteChunk(byte @byte, int line)
    {
        _code.Add(@byte);
        _lines.Add(line);
    }

    internal void DisassembleChunk(string name)
    {
        var header = $" {name} ";
        var len = Math.Max(0, (45 - header.Length));
        header = new string('-', len / 2) + header +
                 new string('-', len - len / 2);
        Console.WriteLine(header);
        var offset = 0;
        while (offset < _code.Count) offset = DisassembleInstruction(offset);
        Console.WriteLine(new string('-', header.Length));
    }

    internal int DisassembleInstruction(int offset)
    {
        Console.Write($"{offset:0000} ");

        if (offset > 0 && _lines[offset] == _lines[offset - 1])
            Console.Write("   | ");
        else
            Console.Write($"{_lines[offset],4} ");

        var instruction = _code[offset];
        switch ((OpCode)instruction)
        {
            case OpCode.Constant:
                return ConstantInstruction("OP_CONSTANT", offset);
            case OpCode.Nil:
                return SimpleInstruction("OP_NIL", offset);
            case OpCode.True:
                return SimpleInstruction("OP_TRUE", offset);
            case OpCode.False:
                return SimpleInstruction("OP_FALSE", offset);
            case OpCode.Pop:
                return SimpleInstruction("OP_POP", offset);
            case OpCode.GetLocal:
                return ByteInstruction("OP_GET_LOCAL", offset);
            case OpCode.SetLocal:
                return ByteInstruction("OP_SET_LOCAL", offset);
            case OpCode.GetGlobal:
                return ConstantInstruction("OP_GET_GLOBAL", offset);
            case OpCode.DefineGlobal:
                return ConstantInstruction("OP_DEFINE_GLOBAL", offset);
            case OpCode.SetGlobal:
                return ConstantInstruction("OP_SET_GLOBAL", offset);
            case OpCode.GetUpvalue:
                return ByteInstruction("OP_GET_UPVALUE", offset);
            case OpCode.SetUpvalue:
                return ByteInstruction("OP_SET_UPVALUE", offset);
            case OpCode.GetProperty:
                return ConstantInstruction("OP_GET_PROPERTY", offset);
            case OpCode.SetProperty:
                return ConstantInstruction("OP_SET_PROPERTY", offset);
            case OpCode.GetSuper:
                return ConstantInstruction("OP_GET_SUPER", offset);
            case OpCode.Equal:
                return SimpleInstruction("OP_EQUAL", offset);
            case OpCode.Greater:
                return SimpleInstruction("OP_GREATER", offset);
            case OpCode.Less:
                return SimpleInstruction("OP_LESS", offset);
            case OpCode.Add:
                return SimpleInstruction("OP_ADD", offset);
            case OpCode.Subtract:
                return SimpleInstruction("OP_SUBTRACT", offset);
            case OpCode.Multiply:
                return SimpleInstruction("OP_MULTIPLY", offset);
            case OpCode.Divide:
                return SimpleInstruction("OP_DIVIDE", offset);
            case OpCode.Not:
                return SimpleInstruction("OP_NOT", offset);
            case OpCode.Negate:
                return SimpleInstruction("OP_NEGATE", offset);
            case OpCode.Print:
                return SimpleInstruction("OP_PRINT", offset);
            case OpCode.Assert:
                return SimpleInstruction("OP_ASSERT", offset);
            case OpCode.Jump:
                return JumpInstruction("OP_JUMP", true, offset);
            case OpCode.JumpIfFalse:
                return JumpInstruction("OP_JUMP_IF_FALSE", true, offset);
            case OpCode.Loop:
                return JumpInstruction("OP_LOOP", false, offset);
            case OpCode.Call:
                return ByteInstruction("OP_CALL", offset);
            case OpCode.Invoke:
                return InvokeInstruction("OP_INVOKE", offset);
            case OpCode.SuperInvoke:
                return InvokeInstruction("OP_SUPER_INVOKE", offset);
            case OpCode.Closure:
            {
                offset++;
                var constant = _code[offset++];
                Console.Write($"{"OP_CLOSURE",-16} {constant,4} ");
                _constants.ReadConstant(constant).Print();
                Console.WriteLine();

                var function = (ObjFunction)_constants.ReadConstant(constant).Obj;
                for (var j = 0; j < function.UpvalueCount; ++j)
                {
                    var isLocal = _code[offset++] != 0;
                    var index = _code[offset++];
                    Console.WriteLine($"{offset - 2:0000}    |                     " +
                                      $"{(isLocal ? "local" : "upvalue")} {index}");
                }

                return offset;
            }
            case OpCode.CloseUpvalue:
                return SimpleInstruction("OP_CLOSE_UPVALUE", offset);
            case OpCode.Return:
                return SimpleInstruction("OP_RETURN", offset);
            case OpCode.Class:
                return ConstantInstruction("OP_CLASS", offset);
            case OpCode.Inherit:
                return SimpleInstruction("OP_INHERIT", offset);
            case OpCode.Method:
                return ConstantInstruction("OP_METHOD", offset);
            default:
                Console.Error.WriteLine($"Unknown opcode {instruction}.");
                return offset + 1;
        }
    }

    private static int SimpleInstruction(string name, int offset)
    {
        Console.WriteLine(name);
        return offset + 1;
    }

    private int ConstantInstruction(string name, int offset)
    {
        var constant = _code[offset + 1];
        Console.Write($"{name,-16} {constant,4} {{");
        _constants.PrintValueWithIdx(constant);
        Console.WriteLine("}");
        return offset + 2;
    }

    private int InvokeInstruction(string name, int offset)
    {
        var constant = _code[offset + 1];
        var argCount = _code[offset + 2];
        Console.Write($"{name,-16} ({argCount} args) {constant,4} {{");
        _constants.PrintValueWithIdx(constant);
        Console.WriteLine("}");
        return offset + 3;
    }

    private int ByteInstruction(string name, int offset)
    {
        var slot = _code[offset + 1];
        Console.WriteLine($"{name,-16} {slot,4}");
        return offset + 2;
    }

    private int JumpInstruction(string name, bool sign, int offset)
    {
        var jump = (ushort)(_code[offset + 1] << 8);
        jump |= _code[offset + 2];
        Console.WriteLine($"{name,-16} {offset,4} -> {offset + 3 + (sign ? 1 : -1) * jump}");
        return offset + 3;
    }

    internal bool PatchJump(int offset)
    {
        var jump = _code.Count - offset - 2;
        // if (jump > ushort.MaxValue) return false;
        _code[offset] = (byte)((jump >> 8) & 0xff);
        _code[offset + 1] = (byte)(jump & 0xff);
        return (jump <= ushort.MaxValue);
    }

    internal int AddConstant(Value value)
    {
        _constants.WriteValueArray(value);
        return _constants.Count - 1;
    }

    internal byte ReadByte(int idx)
    {
        return _code[idx];
    }

    internal Value ReadConstant(int idx)
    {
        return _constants.ReadConstant(idx);
    }

    internal int GetLineNumber(int offset)
    {
        return _lines[offset];
    }
}