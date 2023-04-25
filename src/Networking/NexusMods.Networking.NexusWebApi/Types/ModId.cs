using Vogen;

namespace NexusMods.Networking.NexusWebApi.Types;

/// <summary>
/// An individual mod ID. Unique per game.
/// i.e. Each game has its own set of IDs and starts with 0.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct ModId { }
