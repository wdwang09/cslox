namespace cslox;

internal enum ValueType : byte
{
    Bool,
    Nil,
    Number,
    Obj,
    String
}

internal readonly struct Value
{
    // No arguments means nil.
    public Value()
    {
        _type = ValueType.Nil;
    }

    public Value(bool value)
    {
        _type = ValueType.Bool;
        Boolean = value;
    }

    public Value(double value)
    {
        _type = ValueType.Number;
        Number = value;
    }

    public Value(string value)
    {
        _type = ValueType.String;
        String = value;
    }

    public Value(Obj obj)
    {
        _type = ValueType.Obj;
        _obj = obj;
    }

    internal bool IsBool()
    {
        return _type == ValueType.Bool;
    }

    private bool IsNil()
    {
        return _type == ValueType.Nil;
    }

    internal bool IsNumber()
    {
        return _type == ValueType.Number;
    }

    internal bool IsString()
    {
        return _type == ValueType.String;
    }

    internal bool IsObj()
    {
        return _type == ValueType.Obj;
    }

    internal bool IsObjType(ObjType objType)
    {
        return IsObj() && Obj.Type == objType;
    }

    internal bool IsFalsey()
    {
        return IsNil() || (IsBool() && !Boolean);
    }

    internal bool IsTrue()
    {
        return IsBool() && Boolean;
    }

    internal bool Equal(Value b)
    {
        if (_type != b._type) return false;
        return _type switch
        {
            ValueType.Bool => Boolean == b.Boolean,
            ValueType.Nil => true,
            ValueType.Number => Math.Abs(Number - b.Number) < 1e-9,
            ValueType.String => String == b.String,
            _ => false
        };
    }

    internal void Print()
    {
        if (IsBool())
            Console.Write(Boolean ? "true" : "false");
        else if (IsNil())
            Console.Write("nil");
        else if (IsNumber())
            Console.Write(Number);
        else if (IsString())
            Console.Write("\"" + String + "\"");
        else if (IsObj())
            Console.Write(Obj.ToString());
        else
        {
            Console.Error.Write("[Unsupported]");
        }
    }

    private readonly ValueType _type;
    private bool Boolean { get; } = false;
    internal double Number { get; } = 0;
    internal string String { get; } = "";

    private readonly Obj? _obj = null;

    internal Obj Obj
    {
        get
        {
            if (IsObj()) return _obj!;
            throw new Exception("\"obj\" is null.");
        }
    }
}

public class ValueArray
{
    private readonly List<Value> _values = new();
    public int Count => _values.Count;

    internal void WriteValueArray(Value value)
    {
        _values.Add(value);
    }

    internal void PrintValueWithIdx(int idx)
    {
        _values[idx].Print();
    }

    internal Value ReadConstant(int idx)
    {
        return _values[idx];
    }
}