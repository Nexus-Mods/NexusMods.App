using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;

namespace NexusMods.StandardGameLocators.TestHelpers;

public class ListFilesTool : ITool
{
    public IEnumerable<GameDomain> Domains => new[] { GameDomain.From("stubbed-game") };

    public async Task Execute(Loadout.Model loadout, CancellationToken cancellationToken)
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
    public Task Execute(LoadoutId loadout, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public static readonly GamePath GeneratedFilePath = new(LocationId.Game, "toolFiles.txt");
}
