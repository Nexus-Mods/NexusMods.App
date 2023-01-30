using System.Runtime.InteropServices;
using CliWrap;
using NexusMods.DataModel;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators.TestHelpers;

public class ListFilesTool : ITool
{
    public IEnumerable<GameDomain> Domains => new GameDomain[] {GameDomain.From("stubbed-game")};

    public async Task Execute(Loadout loadout)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
            throw new NotImplementedException();

        var listPath = loadout.Installation.Locations[GameFolderType.Game];
        var outPath = GeneratedFilePath.RelativeTo(listPath);

        var lines = listPath.EnumerateFiles()
            .Select(f => f.RelativeTo(listPath))
            .Select(f => f.ToString())
            .ToArray();

        await outPath.WriteAllLinesAsync(lines);
    }

    public string Name => "List Files";
    public static GamePath GeneratedFilePath = new(GameFolderType.Game, "files.txt");
}