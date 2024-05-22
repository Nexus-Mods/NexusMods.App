using NexusMods.Abstractions.Settings;

namespace NexusMods.App;

/// <summary>
/// Settings that give access to experimental features.
/// </summary>
public record ExperimentalSettings : ISettings
{
    /// <summary>
    /// Enables games that are not enabled by default.
    /// </summary>
    public bool EnableAllGames { get; init; } = true;

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        // TODO: put in some section
        var sectionId = SectionId.DefaultValue;

        return settingsBuilder
            .ConfigureStorageBackend<ExperimentalSettings>(builder => builder.UseJson())
            .AddToUI<ExperimentalSettings>(builder => builder
                .AddPropertyToUI(x => x.EnableAllGames, propertybuilder => propertybuilder
                    .AddToSection(sectionId)
                    .WithDisplayName("Enable Unsupported Games")
                    .WithDescription("When set, 'work-in-progress' games that are not yet fully supported will be enabled in the UI.")
                    .UseBooleanContainer()
                    .RequiresRestart()
                )
            );
    }
}
