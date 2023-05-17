using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusMods.Games.Sifu;

public class Sifu : AGame, ISteamGame, IEpicGame
{
    public IEnumerable<int> SteamIds => new[] { 2138710 };
    public IEnumerable<string> EpicCatalogItemId => new[] { "c80a76de890145edbe0d41679dbccc66" };

    public override string Name => "Sifu";

    public override GameDomain Domain => GameDomain.From("sifu");
    public override GamePath GetPrimaryFile(GameStore store)
    {
        return new(GameFolderType.Game, @"Sifu.exe");
    }
    
    public Sifu(IEnumerable<IGameLocator> gameLocators) : base(gameLocators)
    {
    }

    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(
        IFileSystem fileSystem,
        IGameLocator locator,
        GameLocatorResult installation)
    {
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Game, installation.Path);
    }
}
