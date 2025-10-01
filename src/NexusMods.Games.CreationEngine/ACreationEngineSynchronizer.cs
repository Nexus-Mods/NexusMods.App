using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Sorting;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Games.CreationEngine.Abstractions;
using NexusMods.Games.Generic.IntrinsicFiles;
using NexusMods.Paths;
using NexusMods.Sdk.FileStore;

namespace NexusMods.Games.CreationEngine;

public abstract class ACreationEngineSynchronizer : ALoadoutSynchronizer
{
    private Dictionary<GamePath, IIntrinsicFile> _intrinsicFiles;
    private GamePath _savesPath;
    
    protected ACreationEngineSynchronizer(IServiceProvider provider, ICreationEngineGame game, RelativePath[] iniFiles, GamePath savesPath) : base(provider)
    {
        _savesPath = savesPath;
        var pluginsFile = new PluginsFile(provider.GetRequiredService<ILogger<PluginsFile>>(), game, provider.GetRequiredService<ISorter>());
        _intrinsicFiles = new Dictionary<GamePath, IIntrinsicFile>()
        {
            {pluginsFile.Path, pluginsFile},
        };

        foreach (var iniFile in iniFiles)
        {
            var path = new GamePath(LocationId.Preferences, iniFile);
            _intrinsicFiles.Add(path, new IniFile(path));
        }
    }
 
    protected override Dictionary<GamePath, IIntrinsicFile> IntrinsicFiles(Loadout.ReadOnly _) => _intrinsicFiles;

    protected override bool ShouldIgnorePathWhenIndexing(GamePath path)
    {
        return !path.InFolder(_savesPath);
    }

    public override bool IsIgnoredBackupPath(GamePath path)
    {
        // Don't backup BSA files by default
        return path.Extension == KnownCEExtensions.BSA || path.Extension == KnownCEExtensions.BA2;
    }
}
