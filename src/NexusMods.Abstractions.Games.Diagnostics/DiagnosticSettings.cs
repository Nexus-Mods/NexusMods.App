using JetBrains.Annotations;
using NexusMods.Sdk.Settings;

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
        return settingsBuilder.ConfigureProperty(
            x => x.MinimumSeverity,
            new PropertyOptions<DiagnosticSettings, DiagnosticSeverity>
            {
                Section = Sections.General,
                DisplayName = "Health Check sensitivity",
                DescriptionFactory = _ => "Set the minimum severity for Health Check diagnostics. You will not be notified about diagnostics with a severity lower than the selected level.",
            },
            SingleValueMultipleChoiceContainerOptions.Create<DiagnosticSeverity>(
                valueComparer: EqualityComparer<DiagnosticSeverity>.Default,
                allowedValues: [
                    DiagnosticSeverity.Suggestion,
                    DiagnosticSeverity.Warning,
                    DiagnosticSeverity.Critical,
                ],
                valueToDisplayString: static severity => severity switch
                {
                    // TODO: translate
                    DiagnosticSeverity.Suggestion => "Suggestion",
                    DiagnosticSeverity.Warning => "Warning",
                    DiagnosticSeverity.Critical => "Critical",
                    _ => $"Unknown: {severity}",
                }
            )
        );
    }
}
