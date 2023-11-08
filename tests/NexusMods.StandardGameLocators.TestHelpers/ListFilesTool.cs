using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators.TestHelpers;

public class ListFilesTool : ITool
{
    public IEnumerable<GameDomain> Domains => new[] { GameDomain.From("stubbed-game") };

    public async Task Execute(Loadout loadout, CancellationToken cancellationToken)
    {
        var listPath = loadout.Installation.LocationsRegister[LocationId.Game];
        var outPath = GeneratedFilePath.Combine(listPath);

        var lines = listPath.EnumerateFiles()
            .Select(f => f.RelativeTo(listPath))
            .Select(f => f.ToString())
            .ToArray();

        await outPath.WriteAllLinesAsync(lines, cancellationToken);
    }

    public string Name => "List Files";
    public static GamePath GeneratedFilePath = new(LocationId.Game, "files.txt");
}
