using NexusMods.Abstractions.Settings;

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
        return settingsBuilder.AddToUI<Cyberpunk2077Settings>(builder => builder
            .AddPropertyToUI(x => x.DoFullGameBackup, propertyBuilder => propertyBuilder
                .AddToSection(Sections.Experimental)
                .WithDisplayName("Full game backup: Cyberpunk 2077")
                .WithDescription("Backup all game folders, including the game asset folders. This experimental setting is intended for developers testing the upcoming restore feature. Please note that this will increase disk space usage.")
                .UseBooleanContainer()
            )
        );
        
    }
}
