using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Versions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.EGS;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Paths;
using NexusMods.Games.UnrealEngine.Installers;
using Microsoft.Extensions.DependencyInjection;
using JetBrains.Annotations;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Xbox;
using NexusMods.Games.UnrealEngine.Emitters;

namespace NexusMods.Games.UnrealEngine.Stalker2;

[UsedImplicitly]
public class Stalker2Game(IServiceProvider provider) : AUnrealEngineGame(provider), ISteamGame, IGogGame, IXboxGame, IEpicGame
{
    public static GameDomain DomainStatic => GameDomain.From("stalker2heartofchornobyl");
    public static GameId GameIdStatic => GameId.From(6944);

    public override RelativePath RelPathGameName => "Stalker2";

    public override string Name => "STALKER 2: Heart of Chornobyl";
    public override GameId GameId => GameIdStatic;
    public override SupportType SupportType => SupportType.Official;
    
    public override GamePath GetPrimaryFile(GameStore store) => store == GameStore.XboxGamePass
        //? new GamePath(Constants.BinariesLocationId, "Stalker2-WinGDK-Shipping.exe")
        ? new GamePath(Constants.GameMainLocationId, "gamelaunchhelper.exe")
        : new GamePath(Constants.BinariesLocationId, "Stalker2-Win64-Shipping.exe");

    public IEnumerable<uint> SteamIds => [1643320u];
    public IEnumerable<long> GogIds => [1529799785u, 1630445267u, 1848734189u, 1769087520u];
    public IEnumerable<string> XboxIds => ["GSCGameWorld.S.T.A.L.K.E.R.2HeartofChernobyl"];
    public IEnumerable<string> EpicCatalogItemId => ["Stalker2"];
    
    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<Stalker2Game>("NexusMods.Games.UnrealEngine.Resources.Stalker2.icon.png");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<Stalker2Game>("NexusMods.Games.UnrealEngine.Resources.Stalker2.icon.png");
    
    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
    {
        return new UESynchronizer<Stalker2Settings>(provider, GameIdStatic);
    }
}
