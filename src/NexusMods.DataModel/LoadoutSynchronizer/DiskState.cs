using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.DataModel.LoadoutSynchronizer;

/// <summary>
/// A tree representing the current state of files on disk.
/// </summary>
[JsonConverter(typeof(DiskStateConverter))]
public class DiskState : AGamePathTree<DiskStateEntry>
{
    private DiskState(IEnumerable<KeyValuePair<GamePath, DiskStateEntry>> tree) : base(tree) { }

    public static DiskState Create(IEnumerable<KeyValuePair<GamePath, DiskStateEntry>> tree)
    {
        return new DiskState(tree);
    }
}


class DiskStateConverter : JsonConverter<DiskState>
{
    public override DiskState? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();

        var itms = new List<KeyValuePair<GamePath, DiskStateEntry>>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return DiskState.Create(itms);

            var (locationId, path, hash, size, lastModified) = JsonSerializer.Deserialize<(LocationId, string, Hash, Size, long)>(ref reader, options)!;
            itms.Add(new KeyValuePair<GamePath, DiskStateEntry>(new GamePath(locationId, path),
                new DiskStateEntry
            {
                Hash = hash,
                Size = size,
                LastModified = DateTime.FromFileTimeUtc(lastModified)
            }));
        }

        return DiskState.Create(itms);
    }

    public override void Write(Utf8JsonWriter writer, DiskState value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var (path, entry) in value.GetAllDescendentFiles())
        {
            JsonSerializer.Serialize(writer,
                (path.LocationId, path.Path, entry!.Hash, entry.Size, entry.LastModified.ToFileTimeUtc()));
        }
        writer.WriteEndArray();
    }
}
