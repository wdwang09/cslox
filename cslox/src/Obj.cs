namespace cslox;

internal enum ObjType
{
    Closure,
    Function,
    Native,
    Upvalue
}

internal abstract class Obj
{
    protected Obj(ObjType objType)
    {
        Type = objType;
    }

    internal ObjType Type { get; }
}

internal class ObjFunction : Obj
{
    internal Chunk Chunk { get; }
    internal string Name { get; }
    internal int Arity = 0;
    internal int UpvalueCount = 0;

    internal ObjFunction(string functionName) : base(ObjType.Function)
    {
        Name = functionName;
        Chunk = new Chunk();
    }

    public override string ToString()
    {
        return Name.Length == 0 ? "<script>" : $"<fn {Name}>";
    }
}

internal class ObjClosure : Obj
{
    internal ObjFunction Function { get; }

    internal readonly ObjUpvalue[] Upvalues;
    internal int UpvalueCount { get; }

    internal ObjClosure(ObjFunction function) : base(ObjType.Closure)
    {
        Function = function;
        UpvalueCount = Function.UpvalueCount;
        Upvalues = new ObjUpvalue[UpvalueCount];
    }

    public override string ToString()
    {
        return Function.ToString();
    }
}

internal class ObjUpvalue : Obj
{
    internal int LocationIndex; // if -1 then Closed
    internal Value Closed = new();
    internal ObjUpvalue? Next;

    internal ObjUpvalue(int slot, ObjUpvalue? next = null) : base(ObjType.Upvalue)
    {
        LocationIndex = slot;
        Next = next;
    }

    public override string ToString()
    {
        return "upvalue";
    }
}

internal delegate Value NativeFn(int argCount, List<Value> args);

internal class ObjNative : Obj
{
    internal readonly NativeFn Function;


    internal ObjNative(NativeFn function) : base(ObjType.Native)
    {
        Function = function;
    }

    public override string ToString()
    {
        return "<native fn>";
    }

    internal static Value ClockNative(int argCount, List<Value> args)
    {
        return new Value(DateTime.Now.Ticks / 1e7);
    }
}