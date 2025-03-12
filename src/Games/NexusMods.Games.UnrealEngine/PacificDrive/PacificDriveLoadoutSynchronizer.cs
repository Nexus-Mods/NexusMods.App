using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Games.UnrealEngine.PacificDrive;

public class PacificDriveLoadoutSynchronizer : ALoadoutSynchronizer
{
    private PacificDriveSettings _settings;

    protected internal PacificDriveLoadoutSynchronizer(IServiceProvider provider) : base(provider)
    {
        var settingsManager = provider.GetRequiredService<ISettingsManager>();

        _settings = settingsManager.Get<PacificDriveSettings>();
        settingsManager.GetChanges<PacificDriveSettings>().Subscribe(value => _settings = value);
    }

    public override bool IsIgnoredBackupPath(GamePath path)
    {
        return !_settings.DoFullGameBackup;
    }
    
    public override bool IsIgnoredPath(GamePath path)
    {
        // TODO: Implement path ignore logic once the UESynchronizer is ready.
        return false;
    }
}
