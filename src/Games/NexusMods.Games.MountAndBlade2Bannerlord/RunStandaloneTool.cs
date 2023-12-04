using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;
using NexusMods.Games.MountAndBlade2Bannerlord.Extensions;
using NexusMods.Games.MountAndBlade2Bannerlord.Services;

using static NexusMods.Games.MountAndBlade2Bannerlord.Utils.GamePathProvier;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public class RunStandaloneTool : ITool
{
    private readonly ILogger _logger;
    private readonly LauncherManagerFactory _launcherManagerFactory;

    public string Name => $"Run {MountAndBlade2Bannerlord.DisplayName}";
    public IEnumerable<GameDomain> Domains => new[] { MountAndBlade2Bannerlord.StaticDomain };

    public RunStandaloneTool(ILogger<RunStandaloneTool> logger, LauncherManagerFactory launcherManagerFactory)
    {
        _logger = logger;
        _launcherManagerFactory = launcherManagerFactory;
    }

    public async Task Execute(Loadout loadout, CancellationToken ct)
    {
        if (!loadout.Installation.Is<MountAndBlade2Bannerlord>()) return;

        var store = loadout.Installation.Store;
        var isXbox = store == GameStore.XboxGamePass;
        var hasBLSE = loadout.HasInstalledFile("Bannerlord.BLSE.Shared.dll");
        if (isXbox && !hasBLSE) return; // Not supported.

        var program = hasBLSE
            ? BLSEStandaloneFile(store)
            : PrimaryStandaloneFile(store);
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

        await process.WaitForExitAsync(ct);
        _logger.LogInformation("Finished running {Program}", program);
    }
}
