using JetBrains.Annotations;

namespace NexusMods.Abstractions.Diagnostics;

/// <summary>
/// Represents message data of a <see cref="Diagnostic{TMessageData}"/>.
/// </summary>
[PublicAPI]
public interface IDiagnosticMessageData
{
    /// <summary>
    /// Formats the given message using the current message data.
    /// </summary>
    void Format(IDiagnosticWriter writer, ref DiagnosticWriterState state, DiagnosticMessage message);
}
