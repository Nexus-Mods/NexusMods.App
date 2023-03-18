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

    // TODO: This needs rewritten for new path system. https://github.com/Nexus-Mods/NexusMods.App/issues/210

    /// <summary>
    /// Path to the item within the archive marked by <see cref="Hash"/>.
    /// </summary>
    public readonly RelativePath[] Parts;

    /// <inheritdoc />
    public Extension Extension => Parts.Length > 0
       ? Parts[^1].Extension
       : throw new InvalidOperationException("No path in HashRelativePath");

    /// <inheritdoc />
    public RelativePath FileName => Parts.Length > 0
        ? Parts[^1].FileName
        : throw new InvalidOperationException("No path in HashRelativePath");

    /// <summary/>
    /// <param name="basePath">Base path.</param>
    /// <param name="parts">The individual parts that make up the overall product.</param>
    public HashRelativePath(Hash basePath, params RelativePath[] parts)
    {
        Hash = basePath;
        Parts = parts;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Hash + "|" + string.Join("|", Parts);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is HashRelativePath path && Equals(path);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Parts.Aggregate(Hash.GetHashCode(), (i, path) => i ^ path.GetHashCode());
    }

    /// <inheritdoc />
    public bool Equals(HashRelativePath other)
    {
        if (other.Parts.Length != Parts.Length)
            return false;

        if (other.Hash != Hash)
            return false;

        return ArrayExtensions.AreEqual(Parts, 0, other.Parts, 0, Parts.Length);
    }

    /// <inheritdoc />
    public int CompareTo(HashRelativePath other)
    {
        var init = Hash.CompareTo(other.Hash);
        if (init != 0)
            return init;

        return ArrayExtensions.Compare(Parts, other.Parts);
    }

    #region Operators
#pragma warning disable CS1591
    public static bool operator ==(HashRelativePath a, HashRelativePath b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(HashRelativePath a, HashRelativePath b)
    {
        return !a.Equals(b);
    }
#pragma warning restore CS1591
    #endregion
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

        var lst = new List<RelativePath>();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            lst.Add(reader.GetString()!.ToRelativePath());
            reader.Read();
        }

        return new HashRelativePath(Hash.FromULong(hash), lst.ToArray());
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, HashRelativePath value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue((ulong)value.Hash);
        foreach (var itm in value.Parts)
            JsonSerializer.Serialize(writer, itm, options);
        writer.WriteEndArray();
    }
}
