namespace cslox;

internal enum OpCode : byte
{
    Constant,
    Add,
    Subtract,
    Multiply,
    Divide,
    Negate,
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
        Console.WriteLine($"== {name} ==");
        var offset = 0;
        while (offset < _code.Count) offset = DisassembleInstruction(offset);
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
            case OpCode.Add:
                return SimpleInstruction("OP_ADD", offset);
            case OpCode.Subtract:
                return SimpleInstruction("OP_SUBTRACT", offset);
            case OpCode.Multiply:
                return SimpleInstruction("OP_MULTIPLY", offset);
            case OpCode.Divide:
                return SimpleInstruction("OP_DIVIDE", offset);
            case OpCode.Negate:
                return SimpleInstruction("OP_NEGATE", offset);
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
        Console.Write($"{name,-16} {constant,4} '");
        _constants.PrintValueWithIdx(constant);
        Console.WriteLine("'");
        return offset + 2;
    }

    internal int AddConstant(double value)
    {
        _constants.WriteValueArray(value);
        return _constants.Count - 1;
    }

    internal byte ReadByte(int idx)
    {
        return _code[idx];
    }

    internal double ReadConstant(int idx)
    {
        return _constants.ReadConstant(idx);
    }
}