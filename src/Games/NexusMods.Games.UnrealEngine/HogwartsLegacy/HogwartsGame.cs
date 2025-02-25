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
using NexusMods.Abstractions.Telemetry;

using NexusMods.Games.UnrealEngine;
using NexusMods.Games.UnrealEngine.Emitters;

namespace NexusMods.Games.UnrealEngine.HogwartsLegacy;

[UsedImplicitly]
public class HogwartsLegacyGame(IServiceProvider provider) : AUnrealEngineGame(provider), ISteamGame, IEpicGame
{
    private static GameDomain DomainStatic => GameDomain.From("hogwartslegacy");
    public static GameId GameIdStatic => GameId.From(5113);

    public override string GameFolderName => "Phoenix";
    public override NamedLink UE4SSLink => new("Nexus Mods", NexusModsUrlBuilder.CreateDiagnosticUri(DomainStatic.Value, "942"));
    public override FAesKey? AESKey { get; }
    public override VersionContainer? VersionContainer { get; }

    public override string Name => "Hogwarts Legacy";
    public override GameId GameId => GameIdStatic;
    public override SupportType SupportType => SupportType.Official;
    
    public override GamePath GetPrimaryFile(GameStore store) => new GamePath(Constants.BinariesLocationId, "HogwartsLegacy.exe");

    public IEnumerable<uint> SteamIds => [990080u];

    public IEnumerable<string> EpicCatalogItemId => ["fa4240e57a3c46b39f169041b7811293"];
    
    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<HogwartsLegacyGame>("NexusMods.Games.UnrealEngine.Resources.HogwartsLegacy.icon.png");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<HogwartsLegacyGame>("NexusMods.Games.UnrealEngine.Resources.HogwartsLegacy.icon.png");

    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
    {
        return new HogwartsLegacyLoadoutSynchronizer(provider);
    }
}
