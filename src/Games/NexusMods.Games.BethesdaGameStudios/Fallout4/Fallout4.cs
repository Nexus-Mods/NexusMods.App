using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.GameLocators.Stores.Xbox;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.Fallout4;

public class Fallout4 : ABethesdaGame, ISteamGame, IGogGame, IXboxGame
{
    public override string Name => "Fallout 4";
    public static GameDomain StaticDomain => GameDomain.From("fallout4");
    public override GameDomain Domain => StaticDomain;
    public override GamePath GetPrimaryFile(GameStore store) => new(LocationId.Game, "Fallout4.exe");
    
    public Fallout4(IServiceProvider provider) : base(provider) { }

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
    {
        return new Dictionary<LocationId, AbsolutePath>
        {
            { LocationId.Game, installation.Path },
            { LocationId.AppData, fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("Fallout4") },
        };
    }

    public IEnumerable<uint> SteamIds => new[] { 377160u };
    public IEnumerable<long> GogIds => new[]
    {
        1998527297L, // Fallout 4: Game of the Year Edition
        1408237434L, // Fallout 4 - High Resolution Texture Pack
    };
    public IEnumerable<string> XboxIds => new[] { "BethesdaSoftworks.Fallout4-PC" };
    
    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<Fallout4>(
            "NexusMods.Games.BethesdaGameStudios.Resources.Fallout4.icon.png");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<Fallout4>(
            "NexusMods.Games.BethesdaGameStudios.Resources.Fallout4.game_image.jpg");
}
