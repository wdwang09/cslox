using cslox;

var chunk = new Chunk();

var constant = chunk.AddConstant(1.2);
chunk.WriteChunk((byte)OpCode.OpConstant, 123);
chunk.WriteChunk((byte)constant, 123);

constant = chunk.AddConstant(3.4);
chunk.WriteChunk((byte)OpCode.OpConstant, 123);
chunk.WriteChunk((byte)constant, 123);

chunk.WriteChunk((byte)OpCode.OpAdd, 123);

constant = chunk.AddConstant(5.6);
chunk.WriteChunk((byte)OpCode.OpConstant, 123);
chunk.WriteChunk((byte)constant, 123);

chunk.WriteChunk((byte)OpCode.OpDivide, 123);
chunk.WriteChunk((byte)OpCode.OpNegate, 123);
chunk.WriteChunk((byte)OpCode.OpReturn, 123);
chunk.DisassembleChunk("test chunk");