using NexusMods.Abstractions.Settings;

namespace NexusMods.Games.StardewValley;

public class StardewValleySettings : ISettings
{
    /// <summary>
    /// If true, the contents of the Content folder will not be backed up. If the game updates
    /// the loadout may become invalid. If mods are installed into this folder via the app they
    /// will still be backed up as needed
    /// </summary>
    public bool IgnoreContentFolder { get; set; } = true;


    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder.AddToUI<StardewValleySettings>(builder => builder
            .AddPropertyToUI(x => x.IgnoreContentFolder, propertyBuilder => propertyBuilder
                .AddToSection(Sections.GameSpecific)
                .WithDisplayName("Stardew Valley: Ignore Content Folder")
                .WithDescription("Don't back up the Content folder. If the game updates this may render the loadout invalid.")
                .UseBooleanContainer()
            )
        );
        
    }
}
