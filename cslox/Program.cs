using cslox;

// var chunk = new Chunk();
//
// var constant = chunk.AddConstant(1.2);
// chunk.WriteChunk((byte)OpCode.Constant, 123);
// chunk.WriteChunk((byte)constant, 123);
//
// constant = chunk.AddConstant(3.4);
// chunk.WriteChunk((byte)OpCode.Constant, 123);
// chunk.WriteChunk((byte)constant, 123);
//
// chunk.WriteChunk((byte)OpCode.Add, 123);
//
// constant = chunk.AddConstant(5.6);
// chunk.WriteChunk((byte)OpCode.Constant, 123);
// chunk.WriteChunk((byte)constant, 123);
//
// chunk.WriteChunk((byte)OpCode.Divide, 123);
// chunk.WriteChunk((byte)OpCode.Negate, 123);
// chunk.WriteChunk((byte)OpCode.Return, 123);
// chunk.DisassembleChunk("test chunk");
//
// var vm = new Vm();
// vm.Interpret(chunk);

// =====

var cslox = new CsLox();
switch (args.Length)
{
    case 0:
        cslox.Repl();
        return 0;
    case 1:
        cslox.RunFile(args[0]);
        return 0;
    default:
        Console.Error.WriteLine("Usage: cslox [path]");
        return 64;
}