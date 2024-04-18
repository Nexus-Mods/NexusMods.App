using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.CrossPlatform.Process;

namespace NexusMods.Games.BethesdaGameStudios.SkyrimSpecialEdition;

public class SkyrimSpecialEditionGameTool : RunGameWithScriptExtender<SkyrimSpecialEdition>
{
    public SkyrimSpecialEditionGameTool(IServiceProvider serviceProvider, SkyrimSpecialEdition game)
        : base(serviceProvider, game)
    {
    }
    protected override GamePath ScriptLoaderPath => new(LocationId.Game, "skse64_loader.exe");
}
