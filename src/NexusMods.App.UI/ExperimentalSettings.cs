using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;
using NexusMods.App.BuildInfo;

namespace NexusMods.App.UI;

/// <summary>
/// Settings that give access to experimental features in the UI.
/// </summary>
public record ExperimentalSettings : ISettings
{
    /// <summary>
    /// Enables games that are not enabled by default.
    /// </summary>
    public bool EnableAllGames { get; [UsedImplicitly] set; } = CompileConstants.IsDebug;

    /// <summary>
    /// Enables the ability to have multiple loadouts within the app.
    /// </summary>
    public bool EnableMultipleLoadouts { get; [UsedImplicitly] set; } = CompileConstants.IsDebug;

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        // TODO: put in some section
        var sectionId = SectionId.DefaultValue;

        return settingsBuilder
            .ConfigureStorageBackend<ExperimentalSettings>(builder => builder.UseJson())
            .AddToUI<ExperimentalSettings>(builder => builder
                .AddPropertyToUI(x => x.EnableAllGames, propertyBuilder => propertyBuilder
                    .AddToSection(sectionId)
                    .WithDisplayName("[Unsupported] Enable Unsupported Games")
                    .WithDescription("When set, 'work-in-progress' games that are not yet fully supported will be enabled in the UI.")
                    .UseBooleanContainer()
                    .RequiresRestart()
                )
                .AddPropertyToUI(x => x.EnableMultipleLoadouts, propertyBuilder => propertyBuilder
                    .AddToSection(sectionId)
                    .WithDisplayName("(Experimental) Enable Multiple Loadouts")
                    .WithDescription("When set, you will be able to create multiple loadouts for a game.")
                    .UseBooleanContainer()
                )
            );
    }
}
