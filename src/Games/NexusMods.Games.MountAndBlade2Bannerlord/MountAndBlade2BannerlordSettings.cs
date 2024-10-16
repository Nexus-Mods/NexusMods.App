using NexusMods.Abstractions.Settings;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public class MountAndBlade2BannerlordSettings : ISettings
{
    public bool DoFullGameBackup { get; set; } = false;


    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder.AddToUI<MountAndBlade2BannerlordSettings>(builder => builder
            .AddPropertyToUI(x => x.DoFullGameBackup, propertyBuilder => propertyBuilder
                .AddToSection(Sections.Experimental)
                .WithDisplayName($"Full game backup: {MountAndBlade2Bannerlord.DisplayName}") 
                .WithDescription("Backup all game folders, this will greatly increase disk space usage. Should only be changed before managing the game.")
                .UseBooleanContainer()
            )
        );
        
    }
}
