namespace cslox;

internal enum ObjType
{
    String
}

internal class Obj
{
    protected Obj(ObjType objType)
    {
        Type = objType;
    }

    internal bool IsEqual(Obj b)
    {
        if (IsString() && b.IsString())
        {
            return ((ObjString)this).IsEqual((ObjString)b);
        }

        return false;
    }

    private bool IsString()
    {
        return this is ObjString;
    }

    internal ObjType Type { get; }
}

internal class ObjString : Obj
{
    public ObjString(string str) : base(ObjType.String)
    {
        _chars = str;
    }

    internal bool IsEqual(ObjString b)
    {
        return _chars == b._chars;
    }

    internal static ObjString Concatenate(ObjString a, ObjString b)
    {
        return new ObjString(a._chars + b._chars);
    }

    internal string AsCSharpStringToPrint()
    {
        return "\"" + _chars + "\"";
    }

    private readonly string _chars;
}