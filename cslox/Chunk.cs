namespace cslox;

internal enum OpCode : byte
{
    OpConstant,
    OpReturn
}

public class Chunk
{
    private readonly List<byte> _code = new();
    private readonly ValueArray _constants = new();
    private readonly List<int> _lines = new();

    public void WriteChunk(byte @byte, int line)
    {
        _code.Add(@byte);
        _lines.Add(line);
    }

    public void DisassembleChunk(string name)
    {
        Console.WriteLine($"== {name} ==");
        for (var offset = 0; offset < _code.Count;) offset = DisassembleInstruction(offset);
    }

    private int DisassembleInstruction(int offset)
    {
        Console.Write($"{offset:0000} ");

        if (offset > 0 && _lines[offset] == _lines[offset - 1])
            Console.Write("   | ");
        else
            Console.Write($"{_lines[offset],4} ");

        var instruction = _code[offset];
        switch ((OpCode)instruction)
        {
            case OpCode.OpReturn:
                return SimpleInstruction("OP_RETURN", offset);
            case OpCode.OpConstant:
                return ConstantInstruction("OP_CONSTANT", offset);
            default:
                Console.WriteLine($"Unknown opcode {instruction}");
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
        _constants.PrintValue(constant);
        Console.WriteLine("'");
        return offset + 2;
    }

    public int AddConstant(double value)
    {
        _constants.WriteValueArray(value);
        return _constants.Count - 1;
    }
}