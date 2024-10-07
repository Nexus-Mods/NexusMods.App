using TransparentValueObjects;
namespace NexusMods.Networking.ModUpdates.Structures;

/// <summary>
/// Represents the unique ID of an individual mod in the Nexus Backend.
/// This <see cref="ModId"/> is specific to the <see cref="GameId"/> it belongs to;
/// forming a composite key. 
/// </summary>
[ValueObject<uint>] // Do not modify. Unsafe code relies on this.
public readonly partial struct ModId { }
