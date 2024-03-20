using System.Text;
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
    public virtual string FormatSummary(IDiagnosticWriter writer)
    {
        var sb = new StringBuilder();
        writer.Write(sb, Summary.Value);
        return sb.ToString();
    }

    /// <summary>
    /// Formats <see cref="Details"/> using <paramref name="writer"/>.
    /// </summary>
    public virtual string FormatDetails(IDiagnosticWriter writer)
    {
        var sb = new StringBuilder();
        writer.Write(sb, Details.Value);
        return sb.ToString();
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
    public override string FormatSummary(IDiagnosticWriter writer)
    {
        return MessageData.Format(Summary, writer);
    }

    /// <inheritdoc/>
    public override string FormatDetails(IDiagnosticWriter writer)
    {
        return MessageData.Format(Details, writer);
    }
}
