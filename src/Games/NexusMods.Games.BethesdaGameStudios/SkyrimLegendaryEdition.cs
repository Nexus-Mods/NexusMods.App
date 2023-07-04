using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios;

public class SkyrimLegendaryEdition : AGame, ISteamGame
{
    public SkyrimLegendaryEdition(IEnumerable<IGameLocator> gameLocators) : base(gameLocators) { }
    public override string Name => "Skyrim Legendary Edition";
    
    public static GameDomain StaticDomain => GameDomain.From("skyrim");
    
    public override GameDomain Domain => StaticDomain;
    public override GamePath GetPrimaryFile(GameStore store) => new(GameFolderType.Game, "TESV.exe");
    
    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(
        IFileSystem fileSystem, 
        IGameLocator locator, 
        GameLocatorResult installation)
    {
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Game, installation.Path);
        
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.AppData, fileSystem
            .GetKnownPath(KnownPath.MyGamesDirectory)
            .Combine("Skyrim"));
    }

    public IEnumerable<int> SteamIds => new[] { 72850 };
}
