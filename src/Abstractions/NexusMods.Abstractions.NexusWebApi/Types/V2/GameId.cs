using NexusMods.Abstractions.Games.DTO;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using TransparentValueObjects;
namespace NexusMods.Abstractions.NexusWebApi.Types.V2;

/// <summary>
/// Unique identifier for an individual game hosted on Nexus.
/// </summary>
[ValueObject<uint>] // Matches backend. Do not change.
public readonly partial struct GameId : IAugmentWith<DefaultValueAugment>
{
    /// <inheritdoc/>
    public static GameId DefaultValue => From(default(uint));
    
    /// <summary>
    /// Maps a given <see cref="GameDomain"/> to a <see cref="GameId"/> using known mappings.
    /// This is a TEMPORARY API, until full migration to V2 is complete.
    /// After that it should be REMOVED.
    /// </summary>
    public static GameId FromGameDomain(GameDomain domain)
    {
        return domain.Value switch
        {
            "stardewvalley" => (GameId)1704,
            "cyberpunk2077" => (GameId)3333,
            "baldursgate3" => (GameId)3474,
            "newvegas" => (GameId)130,
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null),
        };
    }
    
    /// <summary>
    /// Maps a given <see cref="GameId"/> to a <see cref="GameDomain"/> using known mappings.
    /// This is a TEMPORARY API, until full migration to V2 is complete.
    /// After that it should be REMOVED.
    /// </summary>
    public GameDomain ToGameDomain()
    {
        var value = Value;
        return value switch
        {
            1704 => GameDomain.From("stardewvalley"),
            3333 => GameDomain.From("cyberpunk2077"),
            3474 => GameDomain.From("baldursgate3"),
            130 => GameDomain.From("newvegas"),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
        };
    }
}

/// <summary>
/// Game ID attribute, for game identifiers from the GraphQL (V2) API.
/// </summary>
public class GameIdAttribute(string ns, string name) 
    : ScalarAttribute<GameId, uint>(ValueTags.UInt32, ns, name)
{
    /// <inheritdoc />
    protected override uint ToLowLevel(GameId value) => value.Value;

    /// <inheritdoc />
    protected override GameId FromLowLevel(uint value, ValueTags tags, AttributeResolver resolver) => GameId.From(value);
}
