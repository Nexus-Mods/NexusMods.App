using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.Extensions;
using NexusMods.Games.MountAndBlade2Bannerlord.Services;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public class RunLauncherTool : ITool
{
    private readonly ILogger _logger;
    private readonly GamePathProvierFactory _gamePathProvierFactory;

    public string Name => $"Run Launcher for {MountAndBlade2Bannerlord.DisplayName}";
    public IEnumerable<GameDomain> Domains => new[] { MountAndBlade2Bannerlord.StaticDomain };

    public RunLauncherTool(ILogger<RunLauncherTool> logger, GamePathProvierFactory gamePathProvierFactory)
    {
        _logger = logger;
        _gamePathProvierFactory = gamePathProvierFactory;
    }

    public async Task Execute(Loadout loadout)
    {
        if (!loadout.Installation.Is<MountAndBlade2Bannerlord>()) return;

        var gamePathProvider = _gamePathProvierFactory.Create(loadout.Installation);

        var isXbox = false; // TODO: From PR #265
        var useVanillaLauncher = false; //TODO: From Options
        var hasBLSE = loadout.HasInstalledFile("Bannerlord.BLSE.Shared.dll");

        var program = hasBLSE
            ? useVanillaLauncher
                ? gamePathProvider.BLSELauncherFile
                : gamePathProvider.BLSELauncherExFile
            : isXbox
                ? gamePathProvider.PrimaryXboxFile
                : gamePathProvider.PrimaryFile;
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
