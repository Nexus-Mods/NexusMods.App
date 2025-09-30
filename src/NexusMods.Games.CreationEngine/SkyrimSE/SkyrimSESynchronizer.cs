using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Games.CreationEngine.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Games.CreationEngine.SkyrimSE;

public class SkyrimSESynchronizer(IServiceProvider provider, ICreationEngineGame game, RelativePath[] iniFiles) : 
    ACreationEngineSynchronizer(provider, game, iniFiles);
