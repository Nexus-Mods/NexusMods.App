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
}
