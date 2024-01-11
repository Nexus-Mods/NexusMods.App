using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.Extensions;
using NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager.Providers;
using static NexusMods.Games.MountAndBlade2Bannerlord.Utils.GamePathProvier;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public class RunStandaloneTool : ITool
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    public string Name => $"Run {MountAndBlade2Bannerlord.DisplayName}";
    public IEnumerable<GameDomain> Domains => new[] { MountAndBlade2Bannerlord.StaticDomain };

    public RunStandaloneTool(ILogger<RunStandaloneTool> logger, IServiceProvider _serviceProvider)
    {
        _logger = logger;
        this._serviceProvider = _serviceProvider;
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

        var launcherState = loadout.Installation.ServiceScope.ServiceProvider.GetRequiredService<LauncherStateProvider>();
        var psi = new ProcessStartInfo(program.ToString())
        {
            Arguments = launcherState.ExecutableParameters
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
