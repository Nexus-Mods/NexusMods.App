using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Games.Obsidian.FalloutNewVegas;

public class FalloutNewVegasSynchronizer : ALoadoutSynchronizer
{
    private FalloutNewVegasSettings _settings;

    protected internal FalloutNewVegasSynchronizer(IServiceProvider provider) : base(provider)
    {
        var settingsManager = provider.GetRequiredService<ISettingsManager>();

        _settings = settingsManager.Get<FalloutNewVegasSettings>();
        settingsManager.GetChanges<FalloutNewVegasSettings>().Subscribe(value => _settings = value);
    }
}
