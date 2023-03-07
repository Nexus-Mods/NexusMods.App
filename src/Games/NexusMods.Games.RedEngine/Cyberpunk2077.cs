using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Games.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.RedEngine;

public class Cyberpunk2077 : AGame, ISteamGame, IGogGame, IEpicGame
{
    public static readonly GameDomain StaticDomain = GameDomain.From("cyberpunk2077");

    public Cyberpunk2077(ILogger<Cyberpunk2077> logger, IEnumerable<IGameLocator> gameLocators) : base(logger, gameLocators)
    {
    }

    public override string Name => "Cyberpunk 2077";
    public override GameDomain Domain => StaticDomain;
    public override GamePath PrimaryFile => new(GameFolderType.Game, @"bin\x64\Cyberpunk2077.exe");
    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(IGameLocator locator, GameLocatorResult installation)
    {
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Game, installation.Path);
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Saves,
            KnownFolders.HomeFolder.CombineUnchecked(@"Saved Games\CD Projekt Red\Cyberpunk 2077"));
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.AppData,
            KnownFolders.HomeFolder.CombineUnchecked(@"AppData\Local\CD Projekt Red\Cyberpunk 2077"));
    }

    public IEnumerable<int> SteamIds => new[] { 1091500 };
    public IEnumerable<long> GogIds => new[] { 2093619782L, 1423049311 };
    public IEnumerable<string> EpicCatalogItemId => new[] { "5beededaad9743df90e8f07d92df153f" };

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<Cyberpunk2077>("NexusMods.Games.RedEngine.Resources.Cyberpunk2077.icon.png");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<Cyberpunk2077>("NexusMods.Games.RedEngine.Resources.Cyberpunk2077.game_image.jpg");
}
