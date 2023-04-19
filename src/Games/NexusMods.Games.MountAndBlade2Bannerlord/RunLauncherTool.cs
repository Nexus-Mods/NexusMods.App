using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.Extensions;
using NexusMods.Games.MountAndBlade2Bannerlord.Utils;

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
        if (!loadout.Installation.Is<MountAndBlade2Bannerlord>()) return;

        var gamePathProvider = GamePathProvier.FromStore(loadout.Installation.Store);

        var isXbox = loadout.Installation.Store == GameStore.XboxGamePass;
        var useVanillaLauncher = false; //TODO: From Options
        var hasBLSE = loadout.HasInstalledFile("Bannerlord.BLSE.Shared.dll");

        var program = hasBLSE
            ? useVanillaLauncher
                ? gamePathProvider.BLSELauncherFile()
                : gamePathProvider.BLSELauncherExFile()
            : isXbox
                ? gamePathProvider.PrimaryXboxFile()
                : gamePathProvider.PrimaryFile();
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
