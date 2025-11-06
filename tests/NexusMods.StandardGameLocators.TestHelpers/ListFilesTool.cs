using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Jobs;
using R3;

namespace NexusMods.StandardGameLocators.TestHelpers;

public class ListFilesTool : ITool
{
    public async Task Execute(Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        var listPath = loadout.InstallationInstance.LocationsRegister[LocationId.Game];
        var outPath = GeneratedFilePath.Combine(listPath);

        var lines = listPath.EnumerateFiles()
            .Select(f => f.RelativeTo(listPath))
            .Select(f => f.ToString())
            .ToArray();

        await outPath.WriteAllLinesAsync(lines, cancellationToken);
    }
    public IJobTask<ITool, Unit> StartJob(Loadout.ReadOnly loadout, IJobMonitor monitor, CancellationToken cancellationToken)
    {
        return monitor.Begin(this, async _ =>
        {
            await Execute(loadout, cancellationToken);
            return Unit.Default;
        });
    }

    public IEnumerable<GameId> GameIds => [];

    public string Name => "List Files";

    public static readonly GamePath GeneratedFilePath = new(LocationId.Game, "toolFiles.txt");
}
