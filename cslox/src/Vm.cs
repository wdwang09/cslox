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
    private readonly double[] _stack = new double[StackMax];
    private Chunk? _chunk;
    private int _ip;
    private int _stackTop;

    public InterpretResult Interpret(Chunk chunk)
    {
        _chunk = chunk;
        _ip = 0;
        return Run();
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
                ValueArray.PrintValue(_stack[i]);
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
                    ValueArray.PrintValue(constant);
                    Console.WriteLine();
                    break;
                case OpCode.Add:
                case OpCode.Subtract:
                case OpCode.Multiply:
                case OpCode.Divide:
                    BinaryOp((OpCode)instruction);
                    break;
                case OpCode.Negate:
                    Push(-Pop());
                    break;
                case OpCode.Return:
                    ValueArray.PrintValue(Pop());
                    Console.WriteLine();
                    return InterpretResult.Ok;
                default:
                    Console.Error.WriteLine($"Unknown opcode {instruction}.");
                    return InterpretResult.CompileError;
            }
        }
    }

    private byte ReadByte()
    {
        return _chunk!.ReadByte(_ip++);
    }

    private double ReadConstant()
    {
        return _chunk!.ReadConstant(ReadByte());
    }

    private void Push(double value)
    {
        _stack[_stackTop++] = value;
    }

    private double Pop()
    {
        return _stack[--_stackTop];
    }

    private void BinaryOp(OpCode instruction)
    {
        var b = Pop();
        var a = Pop();
        switch (instruction)
        {
            case OpCode.Add:
                Push(a + b);
                break;
            case OpCode.Subtract:
                Push(a - b);
                break;
            case OpCode.Multiply:
                Push(a * b);
                break;
            case OpCode.Divide:
                Push(a / b);
                break;
            default:
                throw new Exception($"Wrong opcode {instruction}.");
        }
    }
}