using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Sdk.Settings;
using NexusMods.Sdk;

namespace NexusMods.App.UI.Settings;

/// <summary>
/// Settings that give access to experimental features in the UI.
/// </summary>
public record ExperimentalSettings : ISettings
{
    /// <summary>
    /// Enables games that are not enabled by default.
    /// </summary>
    public bool EnableAllGames { get; [UsedImplicitly] set; } = ApplicationConstants.IsDebug;

    // TODO: remove for GA
    public bool EnableCollectionSharing { get; [UsedImplicitly] set; }

    [JsonIgnore]
    public readonly GameId[] SupportedGames =
    [
        GameId.From(1303), // Stardew Valley 
        GameId.From(3333) //  Cyberpunk
    ];

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder
            .ConfigureBackend(StorageBackendOptions.Use(StorageBackends.Json))
            .ConfigureProperty(
                x => x.EnableAllGames,
                new PropertyOptions<ExperimentalSettings, bool>
                {
                    Section = Sections.Experimental,
                    DisplayName = "Enable unsupported games",
                    DescriptionFactory = _ => "Allows you to manage unsupported games.",
                    RequiresRestart = true,
                },
                new BooleanContainerOptions()
            )
            .ConfigureProperty(
                x => x.EnableCollectionSharing,
                new PropertyOptions<ExperimentalSettings, bool>
                {
                    Section = Sections.Experimental,
                    DisplayName = "Enable sharing collections",
                    DescriptionFactory = _ => "Allows uploading of collections",
                },
                new BooleanContainerOptions()
            );
    }
}
