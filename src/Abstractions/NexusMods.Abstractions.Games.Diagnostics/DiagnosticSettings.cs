using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Abstractions.Diagnostics;

/// <summary>
/// Settings to configure the <see cref="IDiagnosticManager"/>.
/// </summary>
[PublicAPI]
public class DiagnosticSettings : ISettings
{
    /// <summary>
    /// Gets the minimum severity. Diagnostics with a lower severity will be discarded.
    /// </summary>
    public DiagnosticSeverity MinimumSeverity { get; init; } = DiagnosticSeverity.Suggestion;

    /// <inheritdoc/>
    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        // TODO: show in UI
        return settingsBuilder;
    }
}
