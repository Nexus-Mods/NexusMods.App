using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.Extensions;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public class RunBannerlordTool: ITool
{
    private readonly ILogger _logger;
    private readonly LauncherManagerFactory _launcherManagerFactory;

    public string Name => "Run Mount & Blade II: Bannerlord";
    public IEnumerable<GameDomain> Domains => new[] { GameDomain.MountAndBlade2Bannerlord };
    
    public RunBannerlordTool(ILogger<RunBannerlordTool> logger, LauncherManagerFactory launcherManagerFactory)
    {
        _logger = logger;
        _launcherManagerFactory = launcherManagerFactory;
    }

    public async Task Execute(Loadout loadout)
    {
        if (loadout.Installation.Game is not MountAndBlade2Bannerlord mountAndBladeBannerlord) return;
        
        var hasBLSE = loadout.HasModuleId("Bannerlord.BLSE");
        
        var program = hasBLSE
            ? mountAndBladeBannerlord.BLSEStandaloneFile.RelativeTo(loadout.Installation.Locations[GameFolderType.Game])
            : mountAndBladeBannerlord.PrimaryStandaloneFile.RelativeTo(loadout.Installation.Locations[GameFolderType.Game]);
        _logger.LogInformation("Running {Program}", program);

        var launcherManager = _launcherManagerFactory.Get(loadout.Installation);
        var psi = new ProcessStartInfo(program.ToString())
        {
            Arguments = launcherManager.ExecutableParameters
        };
        var process = Process.Start(psi);
        await process.WaitForExitAsync();
        _logger.LogInformation("Finished running {Program}", program);
    }
}