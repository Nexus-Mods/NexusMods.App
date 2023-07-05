using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Diagnostics.References;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Diagnostics;

/// <summary>
/// Represents a diagnostic.
/// </summary>
[PublicAPI]
[JsonName("NexusMods.DataModel.Diagnostic")]
public record Diagnostic : Entity
{
    /// <summary>
    /// Gets the identifier of the diagnostic.
    /// </summary>
    /// <remarks>
    /// Multiple instances can have the same <see cref="Id"/>.
    /// </remarks>
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

    /// <inheritdoc/>
    public override EntityCategory Category => EntityCategory.Diagnostics;

    /// <inheritdoc/>
    public virtual bool Equals(Diagnostic? other)
    {
        return other is not null && DataStoreId.Equals(other.DataStoreId);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return DataStoreId.GetHashCode();
    }
}
