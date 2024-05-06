using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.GameLocators.Stores.Xbox;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.SkyrimSpecialEdition;

public class SkyrimSpecialEdition(IServiceProvider provider) : ABethesdaGame(provider), ISteamGame, IGogGame, IXboxGame
{
    // ReSharper disable InconsistentNaming
    public static readonly Extension ESL = new(".esl");
    public static readonly Extension ESM = new(".esm");
    public static readonly Extension ESP = new(".esp");
    // ReSharper restore InconsistentNaming

    public static readonly HashSet<Extension> PluginExtensions = [ESL, ESM, ESP];
    public static GameDomain StaticDomain => GameDomain.From("skyrimspecialedition");
    public override string Name => "Skyrim Special Edition";
    public override GameDomain Domain => StaticDomain;
    public override GamePath GetPrimaryFile(GameStore store) => new(LocationId.Game, "SkyrimSE.exe");

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem,
        GameLocatorResult installation)
    {
        return new Dictionary<LocationId, AbsolutePath>
        {
            { LocationId.Game, installation.Path },
            {
                LocationId.AppData, installation.Store == GameStore.GOG
                    ? fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory)
                        .Combine("Skyrim Special Edition GOG")
                    : fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("Skyrim Special Edition")
            }
        };
    }
    
    public IEnumerable<uint> SteamIds => new[] { 489830u };

    public IEnumerable<long> GogIds => new long[]
    {
        1711230643, // The Elder Scrolls V: Skyrim Special Edition AKA Base Game
        1801825368, // The Elder Scrolls V: Skyrim Anniversary Edition AKA The Store Bundle
        1162721350 // Upgrade DLC
    };

    public IEnumerable<string> XboxIds => new[] { "BethesdaSoftworks.SkyrimSE-PC" };

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<SkyrimSpecialEdition>(
            "NexusMods.Games.BethesdaGameStudios.Resources.SkyrimSpecialEdition.icon.png");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<SkyrimSpecialEdition>(
            "NexusMods.Games.BethesdaGameStudios.Resources.SkyrimSpecialEdition.game_image.png");

}
