using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;

namespace NexusMods.App.UI.DiagnosticSystem;

internal sealed class DiagnosticWriter : IDiagnosticWriter
{
    private readonly ILogger<DiagnosticWriter> _logger;
    private readonly IValueFormatter[] _formatters;

    public DiagnosticWriter(
        ILogger<DiagnosticWriter> logger,
        IEnumerable<IValueFormatter> formatters)
    {
        _logger = logger;
        _formatters = formatters.ToArray();
    }

    private bool TryGetFormatter<T>([NotNullWhen(true)] out IValueFormatter<T>? formatter) where T : notnull
    {
        formatter = _formatters.OfType<IValueFormatter<T>>().FirstOrDefault();
        if (formatter is not null) return true;

        _logger.LogTrace("Unable to find formatter for type {Type}", typeof(T).ToString());
        return false;
    }

    public void Write<T>(ref readonly DiagnosticWriterState state, T value) where T : notnull
    {
        if (TryGetFormatter<T>(out var formatter))
        {
            formatter.Format(this, stringBuilder, value);
        }
        else
        {
            Write(stringBuilder, value.ToString().AsSpan());
        }
    }

    public void WriteValueType<T>(StringBuilder stringBuilder, T value) where T : struct
    {
        if (TryGetFormatter<T>(out var formatter))
        {
            formatter.Format(this, stringBuilder, value);
        }
        else
        {
            Write(stringBuilder, value.ToString().AsSpan());
        }
    }

    public void Write(StringBuilder stringBuilder, ReadOnlySpan<char> value) => stringBuilder.Append(value);
}
