using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;

namespace NexusMods.App.UI.DiagnosticSystem;

/// <summary>
/// Marker interface for value formatters. This should never be implemented
/// directly, use <see cref="IValueFormatter{T}"/> instead.
/// </summary>
[PublicAPI]
public interface IValueFormatter;

/// <summary>
/// Generic value formatter for values of type <typeparamref name="T"/>.
/// </summary>
[PublicAPI]
public interface IValueFormatter<in T> : IValueFormatter where T : notnull
{
    /// <summary>
    /// Formats the given value of type <typeparamref name="T"/> and writes it
    /// to the given <see cref="IDiagnosticWriter"/>.
    /// </summary>
    void Format(IDiagnosticWriter writer, ref DiagnosticWriterState state, T value);
}
