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
    Return
}

public class Chunk
{
    private readonly List<byte> _code = new();
    private readonly ValueArray _constants = new();
    private readonly List<int> _lines = new();

    internal void WriteChunk(byte @byte, int line)
    {
        _code.Add(@byte);
        _lines.Add(line);
    }

    internal void DisassembleChunk(string name)
    {
        var header = $"===== {name} =====";
        Console.WriteLine(header);
        var offset = 0;
        while (offset < _code.Count) offset = DisassembleInstruction(offset);
        Console.WriteLine(new string('=', header.Length));
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
            case OpCode.Return:
                return SimpleInstruction("OP_RETURN", offset);
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

    private int ByteInstruction(string name, int offset)
    {
        var slot = _code[offset + 1];
        Console.WriteLine($"{name,-16} {slot,4}");
        return offset + 2;
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