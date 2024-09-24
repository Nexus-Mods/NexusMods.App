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
    private static GamePath DataFolder => new(LocationId.Game, "Data");
    private static GamePath PublicPlayerProfiles => new(LocationId.From("PlayerProfiles"), "");
    
    private static GamePath ModSettingsFile => new(LocationId.From("PlayerProfiles"), "modsettings.lsx");
    
    private static Extension PakExtension => new Extension(".pak");
    

    public BaldursGate3Synchronizer(IServiceProvider provider) : base(provider)
    {
        var settingsManager = provider.GetRequiredService<ISettingsManager>();
        _settings = settingsManager.Get<BaldursGate3Settings>();
    }
    
    public override bool IsIgnoredPath(GamePath path)
    {
        // Always ignore all PlayerProfile files except the modsettings file.
        if (path.InFolder(PublicPlayerProfiles))
            return path.Path != ModSettingsFile.Path;
        
        if (_settings.DoFullGameBackup) return false;

        return path.InFolder(DataFolder) && path.Extension == PakExtension;
    }
    
    public override bool IsIgnoredBackupPath(GamePath path)
    {
        if (_settings.DoFullGameBackup) return false;
        
        // Optionally ignore all game folder files for size reasons
        return path.InFolder(GameFolder) ||
               (path.InFolder(PublicPlayerProfiles) && path.Path != ModSettingsFile.Path);
    }
}
