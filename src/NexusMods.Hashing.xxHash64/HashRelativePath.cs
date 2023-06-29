using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Hashing.xxHash64;

/// <summary>
/// A tuple which contains the hash of the source archive a file has came from and its
/// relative path.
/// </summary>
[JsonConverter(typeof(HashRelativePathConverter))]
public readonly struct HashRelativePath : IPath, IEquatable<HashRelativePath>, IComparable<HashRelativePath>
{
    /// <summary>
    /// The hash component of this tuple.
    /// </summary>
    public readonly Hash Hash;

    /// <summary>
    /// Path to the item within the archive marked by <see cref="Hash"/>.
    /// </summary>
    public readonly RelativePath RelativePath;

    /// <inheritdoc />
    public Extension Extension => RelativePath.Extension;

    /// <inheritdoc />
    public RelativePath FileName => RelativePath.FileName;

    /// <summary/>
    /// <param name="basePath">Base path.</param>
    /// <param name="relativePath">The individual parts that make up the overall product.</param>
    public HashRelativePath(Hash basePath, RelativePath relativePath)
    {
        Hash = basePath;
        RelativePath = relativePath;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Hash}|{RelativePath}";


    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is HashRelativePath other && Equals(other);

    /// <inheritdoc />
    public bool Equals(HashRelativePath other)
    {
        return Hash.Equals(other.Hash) && RelativePath.Equals(other.RelativePath);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Hash, RelativePath);
    }

    #region Operators
#pragma warning disable CS1591
    public static bool operator ==(HashRelativePath a, HashRelativePath b) => a.Equals(b);

    public static bool operator !=(HashRelativePath a, HashRelativePath b) => !a.Equals(b);
#pragma warning restore CS1591
    #endregion

    /// <inheritdoc />
    public int CompareTo(HashRelativePath other)
    {
        var hashComparison = Hash.CompareTo(other.Hash);
        if (hashComparison != 0) return hashComparison;
        return RelativePath.CompareTo(other.RelativePath);
    }
}

/// <inheritdoc />
public class HashRelativePathConverter : JsonConverter<HashRelativePath>
{
    /// <inheritdoc />
    public override HashRelativePath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array start");

        reader.Read();

        var hash = reader.GetUInt64();
        reader.Read();

        var relativePath = reader.GetString()!.ToRelativePath();
        reader.Read();

        return new HashRelativePath(Hash.FromULong(hash), relativePath);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, HashRelativePath value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue((ulong)value.Hash);
        writer.WriteStringValue(value.RelativePath.ToString());
        writer.WriteEndArray();
    }
}
