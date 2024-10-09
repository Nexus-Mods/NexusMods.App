using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.MnemonicDB.Abstractions;

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

    public IEnumerable<GameId> GameIds => [ GameId.From(uint.MaxValue) ];

    public string Name => "List Files";

    public static readonly GamePath GeneratedFilePath = new(LocationId.Game, "toolFiles.txt");
}
