namespace cslox;

internal enum FunctionType
{
    Function,
    Initializer,
    Method,
    Script
}

internal struct Local
{
    internal string Name;
    internal int Depth;
    internal bool IsCaptured;
}

internal struct Upvalue
{
    internal byte Index;
    internal bool IsLocal;
}

internal class SubCompiler
{
    internal const int LocalMax = 256;
    internal readonly Local[] Locals = new Local[LocalMax];
    internal int LocalCount;
    internal const int UpvalueMax = 256;
    internal readonly Upvalue[] Upvalues = new Upvalue[UpvalueMax];
    internal int ScopeDepth;
    internal readonly ObjFunction Function;
    internal readonly FunctionType Type;
    internal readonly SubCompiler? Enclosing;

    internal SubCompiler(SubCompiler? compiler, FunctionType type, string functionName = "")
    {
        Enclosing = compiler;
        Type = type;
        if (type == FunctionType.Script)
        {
            functionName = "";
        }

        Function = new ObjFunction(functionName);

        ref var local = ref Locals[LocalCount++];
        local.Depth = 0;
        local.IsCaptured = false;
        local.Name = (type != FunctionType.Function) ? "this" : "";
    }
}

internal class ClassCompiler
{
    internal readonly ClassCompiler? Enclosing;
    internal bool HasSuperclass;

    internal ClassCompiler(ClassCompiler? compiler)
    {
        Enclosing = compiler;
        HasSuperclass = false;
    }
}