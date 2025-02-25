using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Games.UnrealEngine.Stalker2;

public class Stalker2LoadoutSynchronizer : ALoadoutSynchronizer
{
    private Stalker2Settings _settings;

    protected internal Stalker2LoadoutSynchronizer(IServiceProvider provider) : base(provider)
    {
        var settingsManager = provider.GetRequiredService<ISettingsManager>();
        _settings = settingsManager.Get<Stalker2Settings>();
        settingsManager.GetChanges<Stalker2Settings>().Subscribe(value => _settings = value);
    }

    public override bool IsIgnoredBackupPath(GamePath path)
    {
        return !_settings.DoFullGameBackup;
    }
    
    public override bool IsIgnoredPath(GamePath path)
    {
        return false;
        //return _ueSynchronizer.IsIgnored(path, Stalker2Game.GameIdStatic);
    }
}
