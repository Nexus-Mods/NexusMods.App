using TransparentValueObjects;

namespace NexusMods.Networking.NexusWebApi.Types;

/// <summary>
/// Unique identifier for an individual game hosted on Nexus.
/// </summary>
[ValueObject<int>]
public readonly partial struct GameId : IAugmentWith<DefaultValueAugment>
{
    /// <inheritdoc/>
    public static GameId DefaultValue => From(default);
}
