using JetBrains.Annotations;

namespace NexusMods.Abstractions.Diagnostics;

/// <summary>
/// Represents a writer of diagnostic messages.
/// </summary>
/// <remarks>
/// Implementations of this interface are not thread-safe.
/// </remarks>
[PublicAPI]
public interface IDiagnosticWriter : IDisposable
{
    /// <summary>
    /// Writes <paramref name="value"/> to the output.
    /// </summary>
    /// <remarks>
    /// This is used to write field values to the output.
    /// </remarks>
    void Write<T>(T value) where T : notnull;

    /// <inheritdoc cref="Write{T}"/>
    /// <remarks>
    /// This is used to write everything between field values to the output.
    /// </remarks>
    void Write(ReadOnlySpan<char> value);

    /// <summary>
    /// Returns he output of the writer and calls <see cref="Reset"/>.
    /// </summary>
    string GetOutput();

    /// <summary>
    /// Resets the writer to it's initial state.
    /// </summary>
    /// <remarks>
    /// This gets called automatically by <see cref="GetOutput"/>.
    /// </remarks>
    void Reset();
}
