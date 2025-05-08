using NexusMods.Abstractions.Games;

namespace NexusMods.Games.Larian.BaldursGate3.RunGameTools;

public class BG3RunGameTool : RunGameTool<BaldursGate3>
{
    public BG3RunGameTool(IServiceProvider serviceProvider, BaldursGate3 game) : base(serviceProvider, game)
    {
    }
}
