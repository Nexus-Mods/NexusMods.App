using JetBrains.Annotations;

namespace NexusMods.Abstractions.Diagnostics;

/// <summary>
/// Represents the valid modes of a <see cref="IDiagnosticWriter"/>.
/// </summary>
[PublicAPI]
public enum DiagnosticWriterMode
{
    /// <summary>
    /// Plain text.
    /// </summary>
    PlainText = 0,

    /// <summary>
    /// Markdown.
    /// </summary>
    Markdown = 1,
}
