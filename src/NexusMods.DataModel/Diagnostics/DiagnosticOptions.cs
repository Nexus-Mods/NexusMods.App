using JetBrains.Annotations;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Diagnostics;

/// <summary>
/// Options to configure the <see cref="IDiagnosticManager"/>.
/// </summary>
[PublicAPI]
[JsonName("NexusMods.DataModel.Diagnostics.Options")]
public record DiagnosticOptions
{
    /// <summary>
    /// Gets the minimum severity. Diagnostics with a lower severity will be discarded.
    /// </summary>
    public required DiagnosticSeverity MinimumSeverity { get; init; } = DiagnosticSeverity.Suggestion;
}
