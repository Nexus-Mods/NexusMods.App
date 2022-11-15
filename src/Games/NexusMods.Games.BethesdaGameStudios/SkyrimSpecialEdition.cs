using Microsoft.Extensions.Logging;
using NexusMods.Games.Abstractions;
using NexusMods.Interfaces;
using NexusMods.Interfaces.Components;
using NexusMods.Interfaces.StoreLocatorTags;
using NexusMods.Paths;
using NexusMods.StandardGameLocators;

namespace NexusMods.Games.BethesdaGameStudios;

public class SkyrimSpecialEdition : AGame, ISteamGame, IGogGame
{
    public override string Name => "Skyrim Special Edition";
    public override string Slug => "skyrimspecialedition";
    public override GamePath PrimaryFile => new(GameFolderType.Game, "SkyrimSE.exe");


    public SkyrimSpecialEdition(ILogger<SkyrimSpecialEdition> logger, IEnumerable<IGameLocator> gameLocators) : base(logger, gameLocators)
    {
    }
    
    protected override IEnumerable<KeyValuePair<GameFolderType, AbsolutePath>> GetLocations(IGameLocator locator, GameLocatorResult installation)
    {
        yield return new (GameFolderType.Game, installation.Path);
        var profile = locator switch
        {
            SteamLocator => KnownFolders.MyGames.Combine("Skyrim Special Edition"),
            GogLocator => KnownFolders.MyGames.Combine("Skyrim Special Edition GOG"),
            _ => throw new NotImplementedException($"No override for {locator}")
        };
        yield return new(GameFolderType.Preferences, profile);
        yield return new(GameFolderType.Saves, profile.Combine("Saves"));
    }

    public IEnumerable<int> SteamIds => new[] { 489830 };

    public IEnumerable<long> GogIds => new long[]
    {
        1711230643, // The Elder Scrolls V: Skyrim Special Edition AKA Base Game
        1801825368, // The Elder Scrolls V: Skyrim Anniversary Edition AKA The Store Bundle 
        1162721350 // Upgrade DLC
    };
}