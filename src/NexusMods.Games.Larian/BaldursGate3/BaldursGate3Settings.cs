using NexusMods.Abstractions.Settings;

namespace NexusMods.Games.Larian.BaldursGate3;

public class BaldursGate3Settings : ISettings
{
    
    /// <summary>
    /// If true, the contents of the game folder will be backed up. If the game updates
    /// the loadout may become invalid. If mods are installed into this folder via the app they
    /// will still be backed up as needed
    /// </summary>
    public bool DoFullGameBackup { get; set; } = false;
    
    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder.AddToUI<BaldursGate3Settings>(builder => builder
            .AddPropertyToUI(x => x.DoFullGameBackup, propertyBuilder => propertyBuilder
                .AddToSection(Sections.Experimental)
                .WithDisplayName("Full game backup: Baldur's Gate 3")
                .WithDescription("Backup all game folders, this will greatly increase disk space usage. Should only be changed before managing the game.")
                .UseBooleanContainer()
            )
        );
    }
    
}
