using NexusMods.Abstractions.Settings;

namespace NexusMods.App;

#if DEBUG
/// <summary>
/// Settings that are only available in debug releases.
/// </summary>
public record DebugSettings : ISettings
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
            .ConfigureStorageBackend<DebugSettings>(builder => builder.UseJson())
            .AddToUI<DebugSettings>(builder => builder
                .AddPropertyToUI(x => x.EnableAllGames, propertybuilder => propertybuilder
                    .AddToSection(sectionId)
                    .WithDisplayName("Enable all Games")
                    .WithDescription("When set, all games will be enabled in the debug UI.")
                    .UseBooleanContainer()
                    .RequiresRestart()
                )
            );
    }
}
#endif
