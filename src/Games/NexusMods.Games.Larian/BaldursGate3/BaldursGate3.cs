using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Paths;

namespace NexusMods.Games.Larian.BaldursGate3;

public class BaldursGate3 : AGame, ISteamGame, IGogGame
{
    public override string Name => "Baldur's Gate 3";
    
    public IEnumerable<uint> SteamIds => [1086940u];
    public IEnumerable<long> GogIds => [1456460669];

    public static GameDomain GameDomain => GameDomain.From("baldursgate3");
    public override GameDomain Domain => GameDomain;
    
    public BaldursGate3(IServiceProvider provider) : base(provider)
    {
    }
    public override GamePath GetPrimaryFile(GameStore store)
    {
        // TODO: Check linux and osx paths, linux should be the same, osx is unknown
        return new GamePath(LocationId.Game, "bin/bg3.exe");
    }

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
    {
        var result = new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
            { LocationId.From("Mods"), fileSystem.GetKnownPath(KnownPath.HomeDirectory).Combine("Larian Studios/Baldur's Gate 3/Mods") },
            { LocationId.From("PlayerProfiles"), fileSystem.GetKnownPath(KnownPath.HomeDirectory).Combine("Larian Studios/Baldur's Gate 3/PlayerProfiles/Public") },
            { LocationId.From("ScriptExtenderConfig"), fileSystem.GetKnownPath(KnownPath.HomeDirectory).Combine("Larian Studios/Baldur's Gate 3/ScriptExtender") },
        };
        return result;
    }

    /// <inheritdoc />
    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
    {
        // TODO: fill this in for Generic installer
        return [];
    }
    
    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
    {
        return new BaldursGate3Synchronizer(provider);
    }
    
    // TODO: We are using Icon for both Spine and GameWidget and GameImage is unused. We should use GameImage for the GameWidget, but need to update all the games to have better images.
    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<BaldursGate3>("NexusMods.Games.Larian.Resources.BaldursGate3.game_image.jpg");
    
    public override IStreamFactory GameImage => 
        new EmbededResourceStreamFactory<BaldursGate3>("NexusMods.Games.Larian.Resources.BaldursGate3.game_image.jpg");

}
