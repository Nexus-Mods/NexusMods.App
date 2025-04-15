using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Settings;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077;

public class Cyberpunk2077Synchronizer : ALoadoutSynchronizer
{
    private Cyberpunk2077Settings _settings;
    
    /// <summary>
    /// Redmod deploys combined mods to the redmod cache folder
    /// </summary>
    private static GamePath RedModCacheFolder => new(LocationId.Game, "r6/cache/modded");
    
    /// <summary>
    /// Redmod stages the scripts in the redmod/scripts folder
    /// </summary>
    private static GamePath RedModScriptsFolder => new(LocationId.Game, "tools/redmod/scripts");
    
    /// <summary>
    /// Redmod stages the tweaks in the redmod/tweaks folder
    /// </summary>
    private static GamePath RedModTweaksFolder => new(LocationId.Game, "tools/redmod/tweaks");
    
    private static GamePath ArchivePcContentFolder => new(LocationId.Game, "archive/pc/content");
    
    private static GamePath ArchivePcEp1Folder => new(LocationId.Game, "archive/pc/ep1");
    
    
    private readonly RedModDeployTool _redModTool;
    
    protected internal Cyberpunk2077Synchronizer(IServiceProvider provider) : base(provider)
    {
        var settingsManager = provider.GetRequiredService<ISettingsManager>();

        _settings = settingsManager.Get<Cyberpunk2077Settings>();
        settingsManager.GetChanges<Cyberpunk2077Settings>().Subscribe(value => _settings = value);
        _redModTool = provider.GetServices<ITool>().OfType<RedModDeployTool>().First();
    }

    private static readonly GamePath[] IgnoredBackupFolders =
    [
        ArchivePcContentFolder,
        ArchivePcEp1Folder,
    ];

    public override async Task<Loadout.ReadOnly> Synchronize(Loadout.ReadOnly loadout)
    {
        loadout = await base.Synchronize(loadout);
        var hasRedMods = RedModInfoFile.All(loadout.Db)
            .Any(l => l.AsLoadoutFile().AsLoadoutItemWithTargetPath().AsLoadoutItem().LoadoutId == loadout.LoadoutId);
        
        if (!hasRedMods) 
            return loadout;

        await _redModTool.Execute(loadout, CancellationToken.None);
        return await base.Synchronize(loadout);
    }


    public override bool IsIgnoredBackupPath(GamePath path)
    {
        if (_settings.DoFullGameBackup)
            return false;
        
        if (path.LocationId != LocationId.Game)
            return false;
        
        return IgnoredBackupFolders.Any(ignore => path.Path.InFolder(ignore.Path));
    }

}
