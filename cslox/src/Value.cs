namespace cslox;

internal enum ValueType : byte
{
    Bool,
    Nil,
    Number,
    Obj
}

internal readonly struct Value
{
    // No arguments means nil.
    public Value()
    {
        _type = ValueType.Nil;
        Boolean = false;
        Number = 0;
    }

    public Value(bool value)
    {
        _type = ValueType.Bool;
        Boolean = value;
        Number = 0;
    }

    public Value(double value)
    {
        _type = ValueType.Number;
        Boolean = false;
        Number = value;
    }

    public Value(Obj obj)
    {
        _type = ValueType.Obj;
        Boolean = false;
        Number = 0;
        _obj = obj;
    }

    internal bool IsBool()
    {
        return _type == ValueType.Bool;
    }

    internal bool IsNil()
    {
        return _type == ValueType.Nil;
    }

    internal bool IsNumber()
    {
        return _type == ValueType.Number;
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

    internal bool Equal(Value b)
    {
        if (_type != b._type) return false;
        return _type switch
        {
            ValueType.Bool => Boolean == b.Boolean,
            ValueType.Nil => true,
            ValueType.Number => Math.Abs(Number - b.Number) < 1e-9,
            ValueType.Obj => Obj.IsEqual(b.Obj),
            _ => false
        };
    }

    private readonly ValueType _type;
    internal bool Boolean { get; }
    internal double Number { get; }

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
        PrintValue(_values[idx]);
    }

    internal static void PrintValue(Value value)
    {
        if (value.IsBool())
            Console.Write(value.Boolean ? "true" : "false");
        else if (value.IsNil())
            Console.Write("nil");
        else if (value.IsNumber())
            Console.Write(value.Number);
        else if (value.IsObj())
        {
            switch (value.Obj.Type)
            {
                case ObjType.String:
                    Console.Write(((ObjString)value.Obj).AsCSharpStringToPrint());
                    break;
                default:
                    Console.Error.Write("Unsupported.");
                    break;
            }
        }
    }

    internal Value ReadConstant(int idx)
    {
        return _values[idx];
    }
}