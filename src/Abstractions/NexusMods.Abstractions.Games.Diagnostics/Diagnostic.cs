using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics.References;

namespace NexusMods.Abstractions.Diagnostics;

/// <summary>
/// Represents a diagnostic.
/// </summary>
[PublicAPI]
public record Diagnostic
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
    public required Dictionary<DataReferenceDescription, IDataReference> DataReferences { get; init; }

    /// <summary>
    /// Gets the creation time of this diagnostics.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Diagnostic with message data.
/// </summary>
public record Diagnostic<TMessageData> : Diagnostic where TMessageData : struct
{
    /// <summary>
    /// Gets the message data.
    /// </summary>
    public required TMessageData MessageData { get; init; }
}
