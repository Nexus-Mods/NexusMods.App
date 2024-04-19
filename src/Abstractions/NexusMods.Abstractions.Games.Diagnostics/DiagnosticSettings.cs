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
        // TODO: put in some section
        var sectionId = SectionId.DefaultValue;

        return settingsBuilder.AddToUI<DiagnosticSettings>(builder => builder
            .AddPropertyToUI(x => x.MinimumSeverity, propertyBuilder => propertyBuilder
                .AddToSection(sectionId)
                .WithDisplayName("Minimum Severity")
                .WithDescription("Set the minimum Severity for Diagnostics. Any diagnostic with a lower Severity will not appear in the UI.")
                .UseSingleValueMultipleChoiceContainer(
                    valueComparer: EqualityComparer<DiagnosticSeverity>.Default,
                    allowedValues: [
                        DiagnosticSeverity.Suggestion,
                        DiagnosticSeverity.Warning,
                        DiagnosticSeverity.Critical,
                    ],
                    valueToTranslation: static severity => severity switch
                    {
                        // TODO: translate
                        DiagnosticSeverity.Suggestion => "Suggestion",
                        DiagnosticSeverity.Warning => "Warning",
                        DiagnosticSeverity.Critical => "Critical",
                        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null),
                    }
                )
                .RequiresRestart()
            )
        );
    }
}
