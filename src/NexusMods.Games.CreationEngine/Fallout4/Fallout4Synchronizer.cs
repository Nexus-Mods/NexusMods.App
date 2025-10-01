using NexusMods.Abstractions.GameLocators;
using NexusMods.Games.CreationEngine.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Games.CreationEngine.Fallout4;

public class Fallout4Synchronizer(IServiceProvider provider, ICreationEngineGame game, RelativePath[] iniFiles, GamePath savesPath) : 
    ACreationEngineSynchronizer(provider, game, iniFiles, savesPath);
