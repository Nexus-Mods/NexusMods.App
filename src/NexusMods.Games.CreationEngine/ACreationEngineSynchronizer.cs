using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Sorting;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Games.CreationEngine.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.FileStore;

namespace NexusMods.Games.CreationEngine;

public abstract class ACreationEngineSynchronizer : ALoadoutSynchronizer
{
    private IIntrinsicFile[] _intrinsicFiles;
    protected ACreationEngineSynchronizer(IServiceProvider provider, ICreationEngineGame game) : base(provider)
    {
        _intrinsicFiles =
        [
            new PluginsFile(game, provider.GetRequiredService<ISorter>()),
        ];
    }

    protected override IIntrinsicFile[] IntrinsicFiles => _intrinsicFiles;
    
    public override bool IsIgnoredBackupPath(GamePath path)
    {
        // Don't backup BSA files by default
        return path.Extension == KnownCEExtensions.BSA || path.Extension == KnownCEExtensions.BA2;
    }
}
