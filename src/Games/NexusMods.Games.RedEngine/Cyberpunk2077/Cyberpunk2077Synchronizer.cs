using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Games.RedEngine.Cyberpunk2077;

public class Cyberpunk2077Synchronizer : ALoadoutSynchronizer
{
    private Cyberpunk2077Settings _settings;

    protected internal Cyberpunk2077Synchronizer(IServiceProvider provider) : base(provider)
    {
        var settingsManager = provider.GetRequiredService<ISettingsManager>();

        _settings = settingsManager.Get<Cyberpunk2077Settings>();
        settingsManager.GetChanges<Cyberpunk2077Settings>().Subscribe(value => _settings = value);
    }

    private static readonly GamePath[] IgnoredFolders =
    [
        new GamePath(LocationId.Game, "archive/pc/content"),
        new GamePath(LocationId.Game, "archive/pc/ep1"),
    ];


    public override bool IsIgnoredBackupPath(GamePath path)
    {
        if (_settings.DoFullGameBackup)
            return false;
        
        if (path.LocationId != LocationId.Game)
            return false;
        
        return IgnoredFolders.Any(ignore => path.Path.InFolder(ignore.Path));
    }

}
