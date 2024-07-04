using NexusMods.Abstractions.Settings;

namespace NexusMods.Games.RedEngine;

public class Cyberpunk2077Settings : ISettings
{
    /// <summary>
    /// If true, the contents of the Content folder will not be backed up. If the game updates
    /// the loadout may become invalid. If mods are installed into this folder via the app they
    /// will still be backed up as needed
    /// </summary>
    public bool IgnoreContentFolder { get; set; } = true;
    
    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder.AddToUI<Cyberpunk2077Settings>(builder => builder
            .AddPropertyToUI(x => x.IgnoreContentFolder, propertyBuilder => propertyBuilder
                .AddToSection(Sections.GameSpecific)
                .WithDisplayName("Cyberpunk 2077: Ignore Content Folder")
                .WithDescription("Don't back up the game asset folders. If the game updates this may render the loadout invalid.")
                .UseBooleanContainer()
            )
        );
        
    }
}
