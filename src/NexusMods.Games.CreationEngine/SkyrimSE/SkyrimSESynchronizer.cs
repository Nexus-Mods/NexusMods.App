using NexusMods.Abstractions.GameLocators;
using NexusMods.Games.CreationEngine.Abstractions;

namespace NexusMods.Games.CreationEngine.SkyrimSE;

public class SkyrimSESynchronizer : ACreationEngineSynchronizer
{
    public SkyrimSESynchronizer(IServiceProvider provider, ICreationEngineGame game) : base(provider, game)
    {
        
    }
}
