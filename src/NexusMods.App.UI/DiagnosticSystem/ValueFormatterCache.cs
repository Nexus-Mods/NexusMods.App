using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace NexusMods.App.UI.DiagnosticSystem;

internal sealed class ValueFormatterCache
{
    private readonly ILogger<ValueFormatterCache> _logger;
    private readonly IValueFormatter[] _formatters;

    public ValueFormatterCache(
        ILogger<ValueFormatterCache> logger,
        IEnumerable<IValueFormatter> formatters)
    {
        _logger = logger;
        _formatters = formatters.ToArray();
    }

    public bool TryGetFormatter<T>([NotNullWhen(true)] out IValueFormatter<T>? formatter) where T : notnull
    {
        formatter = _formatters.OfType<IValueFormatter<T>>().FirstOrDefault();
        if (formatter is not null) return true;

        _logger.LogDebug("Unable to find formatter for type {Type}", typeof(T).ToString());
        return false;
    }
}
