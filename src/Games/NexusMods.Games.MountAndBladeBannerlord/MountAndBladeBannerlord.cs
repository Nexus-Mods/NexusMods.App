using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Games.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.MountAndBladeBannerlord;

/// <summary>
/// Maintained by the BUTR Team
/// https://github.com/BUTR
/// </summary>
public class MountAndBladeBannerlord : AGame,  ISteamGame, IGogGame, IEpicGame
{
    public IEnumerable<int> SteamIds => new[] { 261550 };
    public IEnumerable<long> GogIds => new long[] { 1802539526, 1564781494 };
    public IEnumerable<string> EpicCatalogItemId => new[] { "Chickadee" };

    public MountAndBladeBannerlord(ILogger<MountAndBladeBannerlord> logger, IEnumerable<IGameLocator> gameLocators) : base(logger, gameLocators)
    {
    }

    public override string Name => "Mount & Blade II: Bannerlord";
    public override GameDomain Domain => GameDomain.From("mountandblade2bannerlord");
    public override GamePath PrimaryFile
    {
        get
        {
            // TODO: Installed BLSE mod will change the primary file, just like SKSE
            return new(GameFolderType.Game, @"bin\Win64_Shipping_Client\TaleWorlds.MountAndBlade.Launcher.exe");
        }
    }

    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(IGameLocator locator, GameLocatorResult installation)
    {
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Game, installation.Path);
        yield return new KeyValuePair<GameFolderType, AbsolutePath>(GameFolderType.Saves, KnownFolders.Documents.Join(@"Mount and Blade II Bannerlord\Game Saves"));
    }
}