using System.Text;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Diagnostics;

/// <summary>
/// Represents a writer of diagnostic messages.
/// </summary>
[PublicAPI]
public interface IDiagnosticWriter
{
    /// <summary>
    /// Writes <paramref name="value"/> to the output.
    /// </summary>
    /// <remarks>
    /// This is used to write field values to the output.
    /// </remarks>
    void Write<T>(ref DiagnosticWriterState state, T value) where T : notnull;

    /// <inheritdoc cref="Write{T}"/>
    /// <remarks>
    /// This is used to write fields that are value types to the output to prevent
    /// boxing.
    /// </remarks>
    void WriteValueType<T>(ref DiagnosticWriterState state, T value) where T : struct;

    /// <inheritdoc cref="Write{T}"/>
    /// <remarks>
    /// This is used to write everything between field values to the output.
    /// </remarks>
    void Write(ref DiagnosticWriterState state, ReadOnlySpan<char> value);

    /// <inheritdoc cref="Write{T}"/>
    /// <remarks>
    /// This is used to write everything between field values to the output.
    /// </remarks>
    void Write(ref DiagnosticWriterState state, string value) => Write(ref state, value.AsSpan());
}
