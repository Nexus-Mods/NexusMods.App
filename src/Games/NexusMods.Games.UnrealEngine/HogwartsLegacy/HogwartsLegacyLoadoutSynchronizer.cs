using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Games.UnrealEngine.HogwartsLegacy;

public class HogwartsLegacyLoadoutSynchronizer : ALoadoutSynchronizer
{
    private HogwartsLegacySettings _settings;
    //private readonly UESynchronizer _ueSynchronizer;

    protected internal HogwartsLegacyLoadoutSynchronizer(IServiceProvider provider) : base(provider)
    {
        var settingsManager = provider.GetRequiredService<ISettingsManager>();
        //_ueSynchronizer = provider.GetRequiredService<UESynchronizer>();
        _settings = settingsManager.Get<HogwartsLegacySettings>();
        settingsManager.GetChanges<HogwartsLegacySettings>().Subscribe(value => _settings = value);
    }

    public override bool IsIgnoredBackupPath(GamePath path)
    {
        return !_settings.DoFullGameBackup;
    }

    public override bool IsIgnoredPath(GamePath path)
    {
        // TODO: Implement path ignore logic once the UESynchronizer is ready.
        return false;
        //return _ueSynchronizer.IsIgnored(path, HogwartsLegacyGame.GameIdStatic);
    }
}
