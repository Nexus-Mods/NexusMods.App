using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.CrossPlatform.Process;

namespace NexusMods.Games.BethesdaGameStudios.SkyrimLegendaryEdition;

public class SkyrimLegendaryEditionGameTool : RunGameWithScriptExtender<SkyrimLegendaryEdition>
{
    public SkyrimLegendaryEditionGameTool(IServiceProvider serviceProvider, SkyrimLegendaryEdition game)
        : base(serviceProvider, game) { }
    
    protected override GamePath ScriptLoaderPath => new(LocationId.Game, "skse_loader.exe");
}
