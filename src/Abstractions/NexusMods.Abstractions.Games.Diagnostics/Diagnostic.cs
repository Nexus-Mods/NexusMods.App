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
    /// Gets the title of the diagnostic.
    /// </summary>
    /// <remarks>
    /// This must not contain any fields. This differs from <see cref="Summary"/>
    /// in that it describes the type of diagnostic, similar to <see cref="Id"/>.
    /// </remarks>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the severity of the diagnostic.
    /// </summary>
    public required DiagnosticSeverity Severity { get; init; }

    /// <summary>
    /// Gets the summary of the diagnostic.
    /// </summary>
    /// <seealso cref="Details"/>
    public required DiagnosticMessage Summary { get; init; }

    /// <summary>
    /// Gets the details of the diagnostic.
    /// </summary>
    /// <seealso cref="Summary"/>
    public required DiagnosticMessage Details { get; init; }

    /// <summary>
    /// Gets all data references.
    /// </summary>
    public required Dictionary<DataReferenceDescription, IDataReference> DataReferences { get; init; }

    /// <summary>
    /// Gets the creation time of this diagnostics.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Formats <see cref="Summary"/> using <paramref name="writer"/>.
    /// </summary>
    public virtual void FormatSummary(IDiagnosticWriter writer)
    {
        writer.Write(Summary.Value);
    }

    /// <summary>
    /// Formats <see cref="Details"/> using <paramref name="writer"/>.
    /// </summary>
    public virtual void FormatDetails(IDiagnosticWriter writer)
    {
        writer.Write(Details.Value);
    }
}

/// <summary>
/// Diagnostic with message data.
/// </summary>
public record Diagnostic<TMessageData> : Diagnostic where TMessageData : struct, IDiagnosticMessageData
{
    /// <summary>
    /// Gets the message data used for <see cref="Diagnostic.Summary"/> and <see cref="Diagnostic.Details"/>.
    /// </summary>
    public required TMessageData MessageData { get; init; }

    /// <inheritdoc/>
    public override void FormatSummary(IDiagnosticWriter writer)
    {
        MessageData.Format(Summary, writer);
    }

    /// <inheritdoc/>
    public override void FormatDetails(IDiagnosticWriter writer)
    {
        MessageData.Format(Details, writer);
    }
}
