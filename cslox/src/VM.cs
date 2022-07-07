namespace cslox;

public enum InterpretResult
{
    InterpretOk,
    InterpretCompileError,
    InterpretRuntimeError
}

public class Vm
{
    private const int StackMax = 256;
    private readonly double[] _stack = new double[StackMax];
    private Chunk _chunk = new();
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
            _chunk.DisassembleInstruction(_ip);
#endif
            var instruction = ReadByte();
            switch ((OpCode)instruction)
            {
                case OpCode.OpConstant:
                    var constant = ReadConstant();
                    Push(constant);
                    ValueArray.PrintValue(constant);
                    Console.WriteLine();
                    break;
                case OpCode.OpAdd:
                case OpCode.OpSubtract:
                case OpCode.OpMultiply:
                case OpCode.OpDivide:
                    BinaryOp((OpCode)instruction);
                    break;
                case OpCode.OpNegate:
                    Push(-Pop());
                    break;
                case OpCode.OpReturn:
                    ValueArray.PrintValue(Pop());
                    Console.WriteLine();
                    return InterpretResult.InterpretOk;
                default:
                    Console.Error.WriteLine($"Unknown opcode {instruction}.");
                    return InterpretResult.InterpretCompileError;
            }
        }
    }

    private byte ReadByte()
    {
        return _chunk.ReadByte(_ip++);
    }

    private double ReadConstant()
    {
        return _chunk.ReadConstant(ReadByte());
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
            case OpCode.OpAdd:
                Push(a + b);
                break;
            case OpCode.OpSubtract:
                Push(a - b);
                break;
            case OpCode.OpMultiply:
                Push(a * b);
                break;
            case OpCode.OpDivide:
                Push(a / b);
                break;
            default:
                throw new Exception($"Wrong opcode {instruction}.");
        }
    }
}