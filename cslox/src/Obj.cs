namespace cslox;

internal enum ObjType
{
    Function,
    Native,
}

internal class Obj
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