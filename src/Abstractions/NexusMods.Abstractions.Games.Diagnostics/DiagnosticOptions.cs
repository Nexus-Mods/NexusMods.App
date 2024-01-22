using System.Collections.Immutable;
using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization.Attributes;

namespace NexusMods.Abstractions.Diagnostics;

/// <summary>
/// Options to configure the <see cref="IDiagnosticManager"/>.
/// </summary>
[PublicAPI]
[JsonName("NexusMods.Abstractions.Diagnostics.DiagnosticOptions")]
public record DiagnosticOptions
{
    /// <summary>
    /// Gets the minimum severity. Diagnostics with a lower severity will be discarded.
    /// </summary>
    public DiagnosticSeverity MinimumSeverity { get; init; } = DiagnosticSeverity.Suggestion;

    /// <summary>
    /// Gets all IDs of diagnostics that should be discarded.
    /// </summary>
    public ImmutableHashSet<DiagnosticId> IgnoredDiagnostics { get; init; } = ImmutableHashSet<DiagnosticId>.Empty;
}
