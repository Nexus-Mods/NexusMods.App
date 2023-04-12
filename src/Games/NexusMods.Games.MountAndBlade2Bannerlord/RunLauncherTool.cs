using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.Extensions;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public class RunLauncherTool : ITool
{
    private readonly ILogger _logger;

    public string Name => $"Run Launcher for {MountAndBlade2Bannerlord.DisplayName}";
    public IEnumerable<GameDomain> Domains => new[] { MountAndBlade2Bannerlord.StaticDomain };

    public RunLauncherTool(ILogger<RunLauncherTool> logger)
    {
        _logger = logger;
    }

    public async Task Execute(Loadout loadout)
    {
        if (loadout.Installation.Game is not MountAndBlade2Bannerlord mountAndBladeBannerlord) return;

        var hasXbox = false; // TODO:
        var hasBLSE = loadout.HasInstalledFile("Bannerlord.BLSE.Shared.dll");
        var hasBUTRLoader = loadout.HasInstalledFile("Bannerlord.BUTRLoader.dll");

        var program = hasBLSE
            ? hasBUTRLoader
                ? mountAndBladeBannerlord.GetBLSELauncherExFile(loadout.Installation).CombineChecked(loadout.Installation.Locations[GameFolderType.Game])
                : mountAndBladeBannerlord.GetBLSELauncherFile(loadout.Installation).CombineChecked(loadout.Installation.Locations[GameFolderType.Game])
            : hasXbox
                ? mountAndBladeBannerlord.GetPrimaryXboxFile(loadout.Installation).CombineChecked(loadout.Installation.Locations[GameFolderType.Game])
                : mountAndBladeBannerlord.GetPrimaryFile(loadout.Installation).CombineChecked(loadout.Installation.Locations[GameFolderType.Game]);
        _logger.LogInformation("Running {Program}", program);

        var psi = new ProcessStartInfo(program.ToString())
        {

        };
        var process = Process.Start(psi);
        if (process is null)
        {
            _logger.LogError("Failed to run {Program}", program);
            return;
        }

        await process.WaitForExitAsync();
        _logger.LogInformation("Finished running {Program}", program);
    }
}
