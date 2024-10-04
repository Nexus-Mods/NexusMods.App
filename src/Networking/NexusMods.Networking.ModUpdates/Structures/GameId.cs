using TransparentValueObjects;
namespace NexusMods.Networking.ModUpdates.Structures;

/// <summary>
/// Represents the unique ID of an individual game in the Nexus Backend. 
/// </summary>
[ValueObject<uint>] // Do not modify. Unsafe code relies on this.
public readonly partial struct GameId { }
