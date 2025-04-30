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
        var sectionId = Sections.General;

        return settingsBuilder.AddToUI<DiagnosticSettings>(builder => builder
            .AddPropertyToUI(x => x.MinimumSeverity, propertyBuilder => propertyBuilder
                .AddToSection(sectionId)
                .WithDisplayName("Health Check sensitivity")
                .WithDescription("Set the minimum severity for Health Check diagnostics. You will not be notified about diagnostics with a severity lower than the selected level.")
                .UseSingleValueMultipleChoiceContainer(
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
            )
        );
    }
}
