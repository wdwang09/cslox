namespace cslox;

internal enum ObjType
{
    BoundMethod,
    Class,
    Closure,
    Function,
    Instance,
    Native,
    UpValue,
}

[Obsolete]
internal class Obj
{
    protected Obj(ObjType objType)
    {
        Type = objType;
    }

    internal ObjType Type { get; }
}