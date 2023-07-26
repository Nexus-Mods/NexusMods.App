using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.RedEngine;

public class Cyberpunk2077 : AGame, ISteamGame, IGogGame, IEpicGame
{
    public static readonly GameDomain StaticDomain = GameDomain.From("cyberpunk2077");
    private readonly IFileSystem _fileSystem;

    public Cyberpunk2077(IEnumerable<IGameLocator> gameLocators, IFileSystem fileSystem) : base(gameLocators)
    {
        _fileSystem = fileSystem;
    }

    public override string Name => "Cyberpunk 2077";
    public override GameDomain Domain => StaticDomain;
    public override GamePath GetPrimaryFile(GameStore store) => new(GameFolderType.Game, "bin/x64/Cyberpunk2077.exe");
    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(IFileSystem fileSystem, IGameLocator locator, GameLocatorResult installation)
    {
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Game, installation.Path);

        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Saves,
            fileSystem
                .GetKnownPath(KnownPath.HomeDirectory)
                .Combine("Saved Games/CD Projekt Red/Cyberpunk 2077")
            );

        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.AppData,
            fileSystem
                .GetKnownPath(KnownPath.LocalApplicationDataDirectory)
                .Combine("CD Projekt Red/Cyberpunk 2077")
        );
    }

    public IEnumerable<uint> SteamIds => new[] { 1091500u };
    public IEnumerable<long> GogIds => new[] { 2093619782L, 1423049311 };
    public IEnumerable<string> EpicCatalogItemId => new[] { "5beededaad9743df90e8f07d92df153f" };

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<Cyberpunk2077>("NexusMods.Games.RedEngine.Resources.Cyberpunk2077.icon.png");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<Cyberpunk2077>("NexusMods.Games.RedEngine.Resources.Cyberpunk2077.game_image.jpg");
}
