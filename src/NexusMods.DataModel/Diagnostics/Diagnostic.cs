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
    /// Gets the globally unique identifier of this object instance.
    /// </summary>
    /// <remarks>
    /// Only a single instance can have this <see cref="Guid"/>.
    /// </remarks>
    /// <seealso cref="Id"/>
    public Guid Guid { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the identifier of the diagnostic.
    /// </summary>
    /// <remarks>
    /// Multiple instances can have the same <see cref="Id"/>.
    /// </remarks>
    /// <seealso cref="Guid"/>
    public required DiagnosticId Id { get; init; }

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

    /// <summary>
    /// Gets the creation time of this diagnostics.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
