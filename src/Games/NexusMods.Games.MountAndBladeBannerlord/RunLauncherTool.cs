using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Games.MountAndBladeBannerlord.Extensions;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBladeBannerlord;

public class RunLauncherTool: ITool
{
    private readonly ILogger _logger;

    public string Name => "Run Launcher for Mount & Blade II: Bannerlord";
    public IEnumerable<GameDomain> Domains => new[] { GameDomain.MBBannerlord };

    public RunLauncherTool(ILogger<RunLauncherTool> logger)
    {
        _logger = logger;
    }

    public async Task Execute(Loadout loadout)
    {
        if (loadout.Installation.Game is not MountAndBladeBannerlord mountAndBladeBannerlord) return;
        
        var hasBLSE = loadout.HasModuleId("Bannerlord.BLSE");
        var hasBUTRLoader = loadout.HasModuleId("Bannerlord.BUTRLoader");
        
        var program = hasBLSE
            ? hasBUTRLoader
                ? mountAndBladeBannerlord.BLSELauncherExFile.RelativeTo(loadout.Installation.Locations[GameFolderType.Game])
                : mountAndBladeBannerlord.BLSELauncherFile.RelativeTo(loadout.Installation.Locations[GameFolderType.Game])
            : mountAndBladeBannerlord.PrimaryFile.RelativeTo(loadout.Installation.Locations[GameFolderType.Game]);
        _logger.LogInformation("Running {Program}", program);
        
        var psi = new ProcessStartInfo(program.ToString())
        {

        };
        var process = Process.Start(psi);
        await process.WaitForExitAsync();
        _logger.LogInformation("Finished running {Program}", program);
    }
}