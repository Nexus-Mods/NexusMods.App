using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Installers.DTO;
using NexusMods.Games.MountAndBlade2Bannerlord.Extensions;
using static NexusMods.Games.MountAndBlade2Bannerlord.Utils.GamePathProvier;

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

    public async Task Execute(Loadout loadout, CancellationToken ct)
    {
        if (!loadout.Installation.Is<MountAndBlade2Bannerlord>()) return;

        var store = loadout.Installation.Store;
        var isXbox = store == GameStore.XboxGamePass;
        var useVanillaLauncher = false; // TODO: From Options
        var hasBLSE = loadout.HasInstalledFile("Bannerlord.BLSE.Shared.dll");
        if (isXbox && !hasBLSE) return; // Not supported.

        var blseExecutable = useVanillaLauncher
            ? BLSELauncherFile(store)
            : BLSELauncherExFile(store);

        var program = isXbox
            ? blseExecutable
            : hasBLSE
                ? blseExecutable
                : PrimaryLauncherFile(store);
        _logger.LogInformation("Running {Program}", program);

        var psi = new ProcessStartInfo(program.ToString());
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
