using System.Text;
using NexusMods.Abstractions.Diagnostics;

namespace NexusMods.App.UI.DiagnosticSystem;

internal sealed class DiagnosticWriter : IDiagnosticWriter
{
    private readonly ValueFormatterCache _formatterCache;
    private readonly StringBuilder _sb = new();

    public DiagnosticWriter(ValueFormatterCache formatterCache)
    {
        _formatterCache = formatterCache;
    }

    public void Write<T>(T value) where T : notnull
    {
        if (_formatterCache.TryGetFormatter<T>(out var formatter))
        {
            formatter.Format(value, this);
        }
        else
        {
            Write(value.ToString().AsSpan());
        }
    }

    public void WriteValueType<T>(T value) where T : struct
    {
        if (_formatterCache.TryGetFormatter<T>(out var formatter))
        {
            formatter.Format(value, this);
        }
        else
        {
            Write(value.ToString().AsSpan());
        }
    }

    public void Write(ReadOnlySpan<char> value) => _sb.Append(value);

    public string GetOutput()
    {
        var res = _sb.ToString();
        Reset();
        return res;
    }

    public void Reset() => _sb.Clear();

    public void Dispose() { }
}
