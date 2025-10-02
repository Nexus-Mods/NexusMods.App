
using NexusMods.Sdk.Settings;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public class BannerlordSettings : ISettings
{
    public bool DoFullGameBackup { get; set; } = false;
    public bool BetaSorting { get; set; } = false;


    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder.ConfigureProperty(
            x => x.DoFullGameBackup,
            new PropertyOptions<BannerlordSettings, bool>
            {
                Section = Sections.Experimental,
                DisplayName = $"Full game backup: {Bannerlord.DisplayName}",
                DescriptionFactory = _ => "Backup all game folders, this will greatly increase disk space usage. Should only be changed before managing the game.",
            },
            new BooleanContainerOptions()
        );
    }
}
