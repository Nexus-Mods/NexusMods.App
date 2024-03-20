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
    /// Constructor.
    /// </summary>
    public DiagnosticWriterState()
    {
        StringBuilder = new StringBuilder();
    }

    /// <summary>
    /// Produces a string output of the state.
    /// </summary>
    public string ToOutput() => StringBuilder.ToString();
}
