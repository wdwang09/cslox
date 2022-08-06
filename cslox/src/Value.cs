namespace cslox;

internal enum ValueType : byte
{
    Bool,
    Nil,
    Number
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
            _ => false
        };
    }

    private readonly ValueType _type;
    internal bool Boolean { get; }
    internal double Number { get; }
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
        else if (value.IsNumber()) Console.Write(value.Number);
    }

    internal Value ReadConstant(int idx)
    {
        return _values[idx];
    }
}