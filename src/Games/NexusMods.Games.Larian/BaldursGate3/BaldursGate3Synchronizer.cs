using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Games.Larian.BaldursGate3;

public class BaldursGate3Synchronizer : ALoadoutSynchronizer
{
    private readonly BaldursGate3Settings _settings;
    
    private static GamePath GameFolder => new(LocationId.Game, "");
    private static GamePath PublicPlayerProfiles => new(LocationId.From("PlayerProfiles"), "");
    
    private static GamePath ModSettingsFile => new(LocationId.From("PlayerProfiles"), "modsettings.lsx");
    

    public BaldursGate3Synchronizer(IServiceProvider provider) : base(provider)
    {
        var settingsManager = provider.GetRequiredService<ISettingsManager>();
        _settings = settingsManager.Get<BaldursGate3Settings>();
    }
    
    public override bool IsIgnoredPath(GamePath path)
    {
        // Always ignore all PlayerProfile files except the modsettings file.
        return path.InFolder(PublicPlayerProfiles) && path.Path != ModSettingsFile.Path;
    }
    
    public override bool IsIgnoredBackupPath(GamePath path)
    {
        if (_settings.DoFullGameBackup) return false;
        
        // Optionally ignore all game folder files for size reasons
        return path.InFolder(GameFolder) ||
               (path.InFolder(PublicPlayerProfiles) && path.Path != ModSettingsFile.Path);
    }
}
