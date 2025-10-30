using System.IO.Hashing;
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

[PublicAPI]
public class Crc32Hasher : IStreamingHasher<Crc32Value, Crc32, Crc32Hasher>
{
    public static Crc32Value Hash(ReadOnlySpan<byte> input)
    {
        var hash = Crc32.HashToUInt32(input);
        return Crc32Value.From(hash);
    }

    public static Crc32 Initialize() => new();

    public static Crc32 Update(Crc32 state, ReadOnlySpan<byte> input)
    {
        state.Append(input);
        return state;
    }

    public static Crc32Value Finish(Crc32 state) => Crc32Value.From(state.GetCurrentHashAsUInt32());
}
