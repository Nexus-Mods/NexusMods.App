using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.Extensions;
using NexusMods.Games.MountAndBlade2Bannerlord.Services;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public class RunStandaloneTool : ITool
{
    private readonly ILogger _logger;
    private readonly GamePathProvierFactory _gamePathProvierFactory;
    private readonly LauncherManagerFactory _launcherManagerFactory;

    public string Name => $"Run {MountAndBlade2Bannerlord.DisplayName}";
    public IEnumerable<GameDomain> Domains => new[] { MountAndBlade2Bannerlord.StaticDomain };

    public RunStandaloneTool(ILogger<RunStandaloneTool> logger, GamePathProvierFactory gamePathProvierFactory, LauncherManagerFactory launcherManagerFactory)
    {
        _logger = logger;
        _gamePathProvierFactory = gamePathProvierFactory;
        _launcherManagerFactory = launcherManagerFactory;
    }

    public async Task Execute(Loadout loadout)
    {
        if (!loadout.Installation.Is<MountAndBlade2Bannerlord>()) return;

        var gamePathProvider = _gamePathProvierFactory.Create(loadout.Installation);

        var isXbox = false; // TODO: From PR #265
        var hasBLSE = loadout.HasInstalledFile("Bannerlord.BLSE.Shared.dll");
        if (isXbox && !hasBLSE) return; // Not supported.

        var program = hasBLSE
            ? gamePathProvider.BLSEStandaloneFile
            : gamePathProvider.PrimaryStandaloneFile;
        _logger.LogInformation("Running {Program}", program);

        var launcherManager = _launcherManagerFactory.Get(loadout.Installation);
        var psi = new ProcessStartInfo(program.ToString())
        {
            Arguments = launcherManager.ExecutableParameters
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
