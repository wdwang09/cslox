namespace cslox;

public class ValueArray
{
    private readonly List<double> _values = new();
    public int Count => _values.Count;

    internal void WriteValueArray(double value)
    {
        _values.Add(value);
    }

    internal void PrintValueWithIdx(int idx)
    {
        PrintValue(_values[idx]);
    }

    internal static void PrintValue(double value)
    {
        Console.Write(value);
    }

    internal double ReadConstant(int idx)
    {
        return _values[idx];
    }
}