using NexusMods.Sdk.Settings;

namespace NexusMods.Games.RedEngine.Cyberpunk2077;

public class Cyberpunk2077Settings : ISettings
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
            new PropertyOptions<Cyberpunk2077Settings, bool>
            {
                Section = Sections.Experimental,
                DisplayName = "Full game backup: Cyberpunk 2077",
                DescriptionFactory = _ => "Backup all game folders, this will greatly increase disk space usage. Should only be changed before managing the game.",
            },
            new BooleanContainerOptions()
        );
    }
}
