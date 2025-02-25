using NexusMods.Abstractions.Settings;

namespace NexusMods.Games.UnrealEngine.HogwartsLegacy;

public class HogwartsLegacySettings : ISettings
{
    /// <summary>
    /// If true, the contents of the Content folder will not be backed up. If the game updates
    /// the loadout may become invalid. If mods are installed into this folder via the app they
    /// will still be backed up as needed
    /// </summary>
    public bool DoFullGameBackup { get; set; } = false;

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder.AddToUI<HogwartsLegacySettings>(builder => builder
            .AddPropertyToUI(x => x.DoFullGameBackup, propertyBuilder => propertyBuilder
                .AddToSection(Sections.Experimental)
                .WithDisplayName("Full game backup: Hogwarts Legacy")
                .WithDescription("Backup all game folders, including the game asset folders. Please note that this will increase disk space usage.")
                .UseBooleanContainer()
            )
        );

    }
}
