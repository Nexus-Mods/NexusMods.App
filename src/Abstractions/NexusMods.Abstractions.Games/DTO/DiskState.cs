using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Abstractions.Games.Legacy;
using NexusMods.Abstractions.Installers.DTO;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using LocationId = NexusMods.Abstractions.Installers.DTO.LocationId;

namespace NexusMods.Abstractions.Games.DTO;

/// <summary>
/// A tree representing the current state of files on disk.
/// </summary>
[JsonConverter(typeof(DiskStateConverter))]
public class DiskState : AGamePathTree<DiskStateEntry>
{
    private DiskState(IEnumerable<KeyValuePair<GamePath, DiskStateEntry>> tree) : base(tree) { }

    /// <summary>
    /// Creates a disk state from a list of files.
    /// </summary>
    /// <param name="tree"></param>
    /// <returns></returns>
    public static DiskState Create(IEnumerable<KeyValuePair<GamePath, DiskStateEntry>> tree)
    {
        return new DiskState(tree);
    }
}


class DiskStateConverter : JsonConverter<DiskState>
{
    public override DiskState? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();

        var itms = new List<KeyValuePair<GamePath, DiskStateEntry>>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return DiskState.Create(itms);

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();

            reader.Read();
            var locationId = LocationId.From(reader.GetString()!);
            reader.Read();
            var path = reader.GetString()!;
            reader.Read();
            var hash = Hash.From(reader.GetUInt64());
            reader.Read();
            var size = Size.From(reader.GetUInt64());
            reader.Read();
            var lastModified = DateTime.FromFileTimeUtc(reader.GetInt64());
            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException();

            itms.Add(new KeyValuePair<GamePath, DiskStateEntry>(new GamePath(locationId, path),
                new DiskStateEntry
            {
                Hash = hash,
                Size = size,
                LastModified = lastModified
            }));
        }

        return DiskState.Create(itms);
    }

    public override void Write(Utf8JsonWriter writer, DiskState value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var (path, entry) in value.GetAllDescendentFiles())
        {
            writer.WriteStartArray();

            writer.WriteStringValue(path.LocationId.Value);
            writer.WriteStringValue(path.Path);
            writer.WriteNumberValue(entry.Hash.Value);
            writer.WriteNumberValue(entry.Size.Value);
            writer.WriteNumberValue(entry.LastModified.ToFileTimeUtc());

            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }
}
