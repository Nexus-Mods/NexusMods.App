using System.Collections;
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

    public override RelativePath RelPathGameName => "Phoenix";
    public override RelativePath? RelPathPakMods => new RelativePath("Content/Paks/WindowsNoEditor");
    public override NamedLink UE4SSLink => new("Nexus Mods", NexusModsUrlBuilder.GetModUri(DomainStatic, ModId.From(942)));

    public override VersionContainer? VersionContainer => new (EGame.GAME_HogwartsLegacy);
    
    public override IEnumerable<FAesKey> AESKeys => new List<FAesKey>
    {
        new ("0x00C0C645000070420000803F6F7E333E0AB8D13E0000803F1CB1F141DE93073D"),
        new ("0xC51B693F6132AD3E5F298B3E0000803F0000803F9432B63D99A6933D0000803F"),
        new ("0x000000419A99993F6F12833B6F12833B6F12833BCDCC4C3FED0DBE3B1B2F5D3C"),
    };

    public override IStreamFactory MemberVariableTemplate =>
        new EmbededResourceStreamFactory<HogwartsLegacyGame>("NexusMods.Games.UnrealEngine.Resources.HogwartsLegacy.MemberVariableLayout.ini");

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
        var ueSync = provider.GetRequiredService<UESynchronizer>();
        ueSync.InitializeSettings<HogwartsLegacySettings>(GameIdStatic);
        return ueSync;
    }
}
