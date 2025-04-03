using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Versions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Games.UnrealEngine.Emitters;
using NexusMods.Paths;

namespace NexusMods.Games.UnrealEngine.Avowed;

[UsedImplicitly]
public class AvowedGame(IServiceProvider provider) : AUnrealEngineGame(provider), ISteamGame
{
    public override RelativePath RelPathGameName => "Alabama";
    public override NamedLink UE4SSLink => Helpers.UE4SSLink;
    public override IEnumerable<FAesKey> AESKeys => new List<FAesKey>
    {
        new ("0xDFA62F3EE8304BBF7A6E153F2F88203F823C47BF0A690D3D4793FB3EFA624F3F"),
    };
    
    public override VersionContainer? VersionContainer => new (EGame.GAME_UE5_3);
    
    public static GameId GameIdStatic => GameId.From(7325);
    public static GameDomain DomainStatic => GameDomain.From("avowed");

    public override string Name => "Avowed";
    public override GameId GameId => GameIdStatic;
    public override SupportType SupportType => SupportType.Official;
    public override GamePath GetPrimaryFile(GameStore store) => new(LocationId.Game, "Avowed.exe");
    public IEnumerable<uint> SteamIds => [2457220u];

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<AvowedGame>("NexusMods.Games.UnrealEngine.Resources.Avowed.icon.png");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<AvowedGame>("NexusMods.Games.UnrealEngine.Resources.Avowed.icon.png");
    
    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
    {
        return new UESynchronizer<AvowedSettings>(provider, GameIdStatic);
    }
}
