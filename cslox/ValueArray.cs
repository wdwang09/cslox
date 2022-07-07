namespace cslox;

public class ValueArray
{
    private readonly List<double> _values = new();
    public int Count => _values.Count;

    public void WriteValueArray(double value)
    {
        _values.Add(value);
    }

    public void PrintValue(int idx)
    {
        Console.Write(_values[idx]);
    }
}