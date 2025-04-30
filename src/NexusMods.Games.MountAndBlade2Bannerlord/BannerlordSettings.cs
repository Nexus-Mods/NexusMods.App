using NexusMods.Abstractions.Settings;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public class BannerlordSettings : ISettings
{
    public bool DoFullGameBackup { get; set; } = false;
    public bool BetaSorting { get; set; } = false;


    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder.AddToUI<BannerlordSettings>(builder => builder
            .AddPropertyToUI(x => x.DoFullGameBackup, propertyBuilder => propertyBuilder
                .AddToSection(Sections.Experimental)
                .WithDisplayName($"Full game backup: {Bannerlord.DisplayName}") 
                .WithDescription("Backup all game folders, this will greatly increase disk space usage. Should only be changed before managing the game.")
                .UseBooleanContainer()
            )
            /* We need to double check whether the Beta Sorting has some actual benefit before enabling it
             * The Beta Sorting is an alternative implementation for sorting Bannerlord mods.
             * The game uses a Topological Sort algorithm to sort the mods
             * The Beta Sorting is a custom implementation of that tries to add the mods in the best position
             * based on multiple loop iteration. I'm not actually sure if it's correctly working in all cases
             */
            /*
            .AddPropertyToUI(x => x.BetaSorting, propertyBuilder => propertyBuilder
                .AddToSection(Sections.Advanced)
                .WithDisplayName($"Beta Sorting: {MountAndBlade2Bannerlord.DisplayName}") 
                .WithDescription("The alternative implementation for sorting Bannerlord mods.")
                .UseBooleanContainer()
            )
            */
        );
        
    }
}
