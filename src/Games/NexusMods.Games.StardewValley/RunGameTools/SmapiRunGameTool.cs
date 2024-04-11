using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.CrossPlatform.Process;

namespace NexusMods.Games.StardewValley.RunGameTools;

/// <summary>
/// This is to run the game or SMAPI using the shell, which allows them to start their own console,
/// allowing users to interact with it.
/// </summary>
public class SmapiRunGameTool : RunGameTool<StardewValley>
{
    public SmapiRunGameTool(
        ILogger<RunGameTool<StardewValley>> logger,
        StardewValley game,
        IProcessFactory processFactory,
        IOSInterop osInterop)
        : base(
            logger,
            game,
            processFactory,
            osInterop
        )
    {
    }
    public override bool UseShell { get; set; } = true;
}
