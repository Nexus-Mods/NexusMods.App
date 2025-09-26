using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Sorting;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Games.CreationEngine.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.FileStore;

namespace NexusMods.Games.CreationEngine;

public abstract class ACreationEngineSynchronizer : ALoadoutSynchronizer
{
    private Dictionary<GamePath, IIntrinsicFile> _intrinsicFiles;
    protected ACreationEngineSynchronizer(IServiceProvider provider, ICreationEngineGame game) : base(provider)
    {
        var pluginsFile = new PluginsFile(provider.GetRequiredService<ILogger<PluginsFile>>(), game, provider.GetRequiredService<ISorter>());
        _intrinsicFiles = new Dictionary<GamePath, IIntrinsicFile>()
        {
            {pluginsFile.Path, pluginsFile},
        };
    }
 
    protected override Dictionary<GamePath, IIntrinsicFile> IntrinsicFiles(Loadout.ReadOnly _) => _intrinsicFiles;
    
    public override bool IsIgnoredBackupPath(GamePath path)
    {
        // Don't backup BSA files by default
        return path.Extension == KnownCEExtensions.BSA || path.Extension == KnownCEExtensions.BA2;
    }
}
