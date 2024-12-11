using TransparentValueObjects;

namespace NexusMods.Abstractions.Hashes;

/// <summary>
/// A value representing a 32-bit Cyclic Redundancy Check (CRC) hash.
/// </summary>
[ValueObject<uint>]
public readonly partial struct Crc32
{
}
