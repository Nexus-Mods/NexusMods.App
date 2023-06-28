using JetBrains.Annotations;
using NexusMods.DataModel.Diagnostics.References;

namespace NexusMods.DataModel.Diagnostics;

/// <summary>
/// Represents a diagnostic.
/// </summary>
[PublicAPI]
public record Diagnostic
{
    /// <summary>
    /// Gets the creation time of this diagnostics.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the severity of the diagnostic.
    /// </summary>
    public required DiagnosticSeverity Severity { get; init; }

    /// <summary>
    /// Gets the message of the diagnostic.
    /// </summary>
    public required DiagnosticMessage Message { get; init; }

    /// <summary>
    /// Gets all data references.
    /// </summary>
    public required IReadOnlyList<IDataReference> DataReferences { get; init; }
}
