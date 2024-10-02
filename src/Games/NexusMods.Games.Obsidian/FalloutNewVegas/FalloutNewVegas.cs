using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.EGS;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.GameLocators.Stores.Xbox;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Paths;

// The argument could be made that the package should be Bethesda not Obsidian... todo someone confirm preferred package name
namespace NexusMods.Games.Obsidian.FalloutNewVegas;

public class FalloutNewVegas : AGame, ISteamGame, IGogGame, IXboxGame, IEpicGame
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOSInformation _osInformation;

    public FalloutNewVegas(IServiceProvider provider, IServiceProvider serviceProvider, IOSInformation osInformation) : base(provider)
    {
        _serviceProvider = serviceProvider;
        _osInformation = osInformation;
    }
    
    private static readonly string _name = "Fallout New Vegas";

    public static string GameName => _name; // used to statically reference the game name elsewhere... in case the name changes? idk.
    public override string Name => _name;
    public override GameDomain Domain => GameDomain.From("falloutnv");

#region File Information

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
    {
        var result = new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
        };
        return result;
    }
    
    public override GamePath GetPrimaryFile(GameStore store)
    {
        return store.ToString() switch
        {
            "Epic Games Store" => new GamePath(LocationId.Game, "/Fallout New Vegas English/FalloutNV.exe"), // todo going to need this to handle the language... somehow?
            _ => new GamePath(LocationId.Game, "FalloutNV.exe"),
        };
    }

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations) => ModInstallDestinationHelpers.GetCommonLocations(locations);

    protected override Version GetVersion(GameLocatorResult installation)
    {
        return base.GetVersion(installation);
    }

#endregion

#region Game IDs

    public IEnumerable<uint> SteamIds => new List<uint> { 22380u };
    public IEnumerable<long> GogIds => new List<long> { 1207658921 }; //todo need correct ID. I don't own this.
    public IEnumerable<string> XboxIds => new List<string> { "9P4P6BZQ9V6M" }; //todo need correct ID
    public IEnumerable<string> EpicCatalogItemId => new List<string> { "dabb52e328834da7bbe99691e374cb84" };
#endregion
    
    public override IStreamFactory GameImage => new EmbededResourceStreamFactory<FalloutNewVegas>("NexusMods.Games.Obsidian.Resources.FalloutNewVegas.game_image.jpg");
    public override IStreamFactory Icon => new EmbededResourceStreamFactory<FalloutNewVegas>("NexusMods.Games.Obsidian.Resources.FalloutNewVegas.icon.jpg");

}
