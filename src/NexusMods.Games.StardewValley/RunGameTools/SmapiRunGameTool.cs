using NexusMods.Abstractions.Games;

namespace NexusMods.Games.StardewValley.RunGameTools;

/// <summary>
/// This is to run the game or SMAPI using the shell, which allows them to start their own console,
/// allowing users to interact with it.
/// </summary>
public class SmapiRunGameTool : RunGameTool<StardewValley>
{
    public SmapiRunGameTool(IServiceProvider serviceProvider, StardewValley game)
        : base(serviceProvider, game)
    {
    }

    protected override bool UseShell { get; set; } = true;
}
