using System.Text;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Diagnostics;

/// <summary>
/// Represents the state used in the <see cref="IDiagnosticWriter"/>.
/// </summary>
[PublicAPI]
public readonly struct DiagnosticWriterState
{
    /// <summary>
    /// <see cref="StringBuilder"/> used to create the output string.
    /// </summary>
    public readonly StringBuilder StringBuilder;

    /// <summary>
    /// Gets the mode of the writer.
    /// </summary>
    public readonly DiagnosticWriterMode Mode;

    /// <summary>
    /// Constructor.
    /// </summary>
    public DiagnosticWriterState(DiagnosticWriterMode mode, int capacity = 16)
    {
        StringBuilder = new StringBuilder(capacity);
        Mode = mode;
    }

    /// <summary>
    /// Produces a string output of the state.
    /// </summary>
    public string ToOutput() => StringBuilder.ToString();
}
