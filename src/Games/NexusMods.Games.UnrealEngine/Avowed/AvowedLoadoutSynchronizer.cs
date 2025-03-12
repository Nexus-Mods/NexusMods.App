using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Games.UnrealEngine.Avowed;

public class AvowedLoadoutSynchronizer : ALoadoutSynchronizer
{
    private AvowedSettings _settings;
    //private readonly UESynchronizer _ueSynchronizer;

    protected internal AvowedLoadoutSynchronizer(IServiceProvider provider) : base(provider)
    {
        
        var settingsManager = provider.GetRequiredService<ISettingsManager>();
        _settings = settingsManager.Get<AvowedSettings>();
        settingsManager.GetChanges<AvowedSettings>().Subscribe(value => _settings = value);
    }

    public override bool IsIgnoredBackupPath(GamePath path)
    {
        return !_settings.DoFullGameBackup;
    }

    public override bool IsIgnoredPath(GamePath path)
    {
        return false;
        //return _ueSynchronizer.IsIgnored(path, AvowedGame.GameIdStatic);
    }
}
