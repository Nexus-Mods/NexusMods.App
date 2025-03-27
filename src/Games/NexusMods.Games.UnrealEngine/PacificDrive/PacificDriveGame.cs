using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Versions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.EGS;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using JetBrains.Annotations;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Games.UnrealEngine.Emitters;

namespace NexusMods.Games.UnrealEngine.PacificDrive;

[UsedImplicitly]
public class PacificDriveGame : AUnrealEngineGame, ISteamGame, IEpicGame
{
    public override string GameFolderName => "PenDriverPro";
    
    public static GameDomain DomainStatic => GameDomain.From("pacificdrive");
    private readonly IServiceProvider _serviceProvider;

    public PacificDriveGame(IEnumerable<IGameLocator> gameLocators, IServiceProvider provider) : base(provider)
    {
        _serviceProvider = provider;
    }

    public override string Name => "Pacific Drive";
    public override GameId GameId => GameId.From(6169);
    public override SupportType SupportType => SupportType.Community;
    public override GamePath GetPrimaryFile(GameStore store) => new(Constants.BinariesLocationId, "PenDriverPro-Win64-Shipping.exe");

    public IEnumerable<uint> SteamIds => [1458140u];
    public IEnumerable<string> EpicCatalogItemId => ["c75f6d17cb064f52bbf07c61df32e30f"];

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<PacificDriveGame>("NexusMods.Games.UnrealEngine.Resources.PacificDrive.icon.png");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<PacificDriveGame>("NexusMods.Games.UnrealEngine.Resources.PacificDrive.game_image.jpg");

    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
    {
        return new PacificDriveLoadoutSynchronizer(provider);
    }
}
