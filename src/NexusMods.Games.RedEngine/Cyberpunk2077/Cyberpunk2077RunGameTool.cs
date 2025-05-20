using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Games.RedEngine.Cyberpunk2077;

public class Cyberpunk2077RunGameTool : RunGameTool<Cyberpunk2077Game>
{
    public Cyberpunk2077RunGameTool(IServiceProvider serviceProvider, Cyberpunk2077Game game) : base(serviceProvider, game) { }
    protected override bool UseShell { get; set; } = false;
    
    private const string EnableRedModModdingArgument = "-modded";
    
    public override async Task Execute(Loadout.ReadOnly loadout, CancellationToken cancellationToken, string[]? commandLineArgs)
    {
        commandLineArgs ??= [];

        // Add `-modded` argument to the command line, this is needed to make the game load Redmod mods:
        // https://wiki.redmodding.org/cyberpunk-2077-modding/for-mod-users/users-modding-cyberpunk-2077/redmod/usage#starting-a-modded-game-manually
        var args = commandLineArgs.Concat([EnableRedModModdingArgument]).ToArray();
        
        await base.Execute(loadout, cancellationToken, args).ConfigureAwait(false);
    }
}
