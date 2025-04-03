using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Settings;
using NexusMods.Paths;

namespace NexusMods.Games.Larian.BaldursGate3;

public class BaldursGate3Synchronizer : ALoadoutSynchronizer
{
    private readonly BaldursGate3Settings _settings;

    private static GamePath GameFolder => new(LocationId.Game, "");
    private static GamePath PublicPlayerProfiles => new(Bg3Constants.PlayerProfilesLocationId, "");
    private static GamePath ModSettingsFile => new(Bg3Constants.PlayerProfilesLocationId, "modsettings.lsx");

    public BaldursGate3Synchronizer(IServiceProvider provider) : base(provider)
    {
        var settingsManager = provider.GetRequiredService<ISettingsManager>();
        _settings = settingsManager.Get<BaldursGate3Settings>();
    }

    protected override bool ShouldIgnorePathWhenIndexing(GamePath path)
    {
        // ignore all files inside the public player profiles directory except the modsettings.lsx file
        if (path.InFolder(PublicPlayerProfiles)) return !path.Equals(ModSettingsFile);
        return false;
    }

    public override bool IsIgnoredBackupPath(GamePath path)
    {
        if (_settings.DoFullGameBackup) return false;
        return path.InFolder(GameFolder) || (path.InFolder(PublicPlayerProfiles) && path.Path != ModSettingsFile.Path);
    }
}
