using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Hashing.xxHash64;

[JsonConverter(typeof(HashRelativePathConverter))]
public readonly struct HashRelativePath : IPath, IEquatable<HashRelativePath>, IComparable<HashRelativePath>
{
    public readonly Hash Hash;
    public readonly RelativePath[] Parts;


     public Extension Extension => Parts.Length > 0
        ? Parts[^1].Extension
        : throw new InvalidOperationException("No path in HashRelativePath");

    public RelativePath FileName => Parts.Length > 0
        ? Parts[^1].FileName
        : throw new InvalidOperationException("No path in HashRelativePath");

    public HashRelativePath(Hash basePath, params RelativePath[] parts)
    {
        Hash = basePath;
        Parts = parts;
    }

    public override string ToString()
    {
        return Hash + "|" + string.Join("|", Parts);
    }

    public override bool Equals(object? obj)
    {
        return obj is HashRelativePath path && Equals(path);
    }

    public override int GetHashCode()
    {
        return Parts.Aggregate(Hash.GetHashCode(), (i, path) => i ^ path.GetHashCode());
    }

    public bool Equals(HashRelativePath other)
    {
        if (other.Parts.Length != Parts.Length) return false;
        if (other.Hash != Hash) return false;
        return ArrayExtensions.AreEqual(Parts, 0, other.Parts, 0, Parts.Length);
    }

    public int CompareTo(HashRelativePath other)
    {
        var init = Hash.CompareTo(other.Hash);
        if (init != 0) return init;
        return ArrayExtensions.Compare(Parts, other.Parts);
    }

    public static bool operator ==(HashRelativePath a, HashRelativePath b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(HashRelativePath a, HashRelativePath b)
    {
        return !a.Equals(b);
    }
}

public class HashRelativePathConverter : JsonConverter<HashRelativePath>
{
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

    public override void Write(Utf8JsonWriter writer, HashRelativePath value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue((ulong)value.Hash);
        foreach (var itm in value.Parts)
            JsonSerializer.Serialize(writer, itm, options);
        writer.WriteEndArray();
    }
}