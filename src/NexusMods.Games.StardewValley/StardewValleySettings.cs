using NexusMods.Sdk.Settings;

namespace NexusMods.Games.StardewValley;

public class StardewValleySettings : ISettings
{
    /// <summary>
    /// If true, the contents of the Content folder will not be backed up. If the game updates
    /// the loadout may become invalid. If mods are installed into this folder via the app they
    /// will still be backed up as needed
    /// </summary>
    public bool DoFullGameBackup { get; set; } = false;

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder.ConfigureProperty(
            x => x.DoFullGameBackup,
            new PropertyOptions<StardewValleySettings, bool>
            {
                Section = Sections.Experimental,
                DisplayName = "Full game backup: Stardew Valley",
                DescriptionFactory = _ => "Backup all game folders, this will greatly increase disk space usage. Should only be changed before managing the game.",
            },
            new BooleanContainerOptions()
        );
    }
}
