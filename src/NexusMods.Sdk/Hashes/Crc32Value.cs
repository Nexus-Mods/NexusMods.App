using System.Text.Json.Serialization;
using JetBrains.Annotations;
using TransparentValueObjects;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// A value representing a 32-bit Cyclic Redundancy Check (CRC) hash.
/// </summary>
[PublicAPI]
[ValueObject<uint>]
[JsonConverter(typeof(Crc32JsonConverter))]
public readonly partial struct Crc32Value;
