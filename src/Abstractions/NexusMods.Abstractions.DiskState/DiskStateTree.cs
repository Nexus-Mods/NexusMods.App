using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Trees;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.DataModel.Converters;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Abstractions.DiskState;

/// <summary>
/// A tree representing the current state of files on disk.
/// </summary>
[JsonConverter(typeof(DiskStateConverter))]
public class DiskStateTree : AGamePathNodeTree<DiskStateEntry>
{
    private DiskStateTree(IEnumerable<KeyValuePair<GamePath, DiskStateEntry>> tree) : base(tree) { }

    /// <summary>
    /// The associated loadout id.
    /// </summary>
    public IId LoadoutRevision { get; set; } = IdEmpty.Empty;
    
    /// <summary>
    ///     Creates a disk state from a list of files.
    /// </summary>
    public static DiskStateTree Create(IEnumerable<KeyValuePair<GamePath, DiskStateEntry>> items) => new(items);
}

class DiskStateConverter : JsonConverter<DiskStateTree>
{
    public override DiskStateTree Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();
        reader.Read();
        
        if (reader.TokenType != JsonTokenType.PropertyName)
            throw new JsonException();
        if (reader.GetString() != "loadoutRevision")
            throw new JsonException();
        reader.Read();
        
        var revId = options.Converters.OfType<IdJsonConverter>().First().Read(ref reader, typeof(IId), options);
        
        if (reader.TokenType != JsonTokenType.PropertyName)
            throw new JsonException();
        if (reader.GetString() != "files")
            throw new JsonException();
        reader.Read();
        
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();

        var itms = new List<KeyValuePair<GamePath, DiskStateEntry>>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
               break;

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
        reader.Read();
        
        if(reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException();
        
        var tree = DiskStateTree.Create(itms);
        tree.LoadoutRevision = revId;
        return tree;
    }

    public override void Write(Utf8JsonWriter writer, DiskStateTree value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("loadoutRevision");
        options.Converters.OfType<IdJsonConverter>().First().Write(writer, value.LoadoutRevision, options);
        
        writer.WritePropertyName("files");
        writer.WriteStartArray();
        foreach (var boxed in value.GetAllDescendentFiles())
        {
            ref var item = ref boxed.Item;
            writer.WriteStartArray();

            writer.WriteStringValue(item.Id.Value);
            writer.WriteStringValue(item.ReconstructPath());
            writer.WriteNumberValue(item.Value.Hash.Value);
            writer.WriteNumberValue(item.Value.Size.Value);
            writer.WriteNumberValue(item.Value.LastModified.ToFileTimeUtc());

            writer.WriteEndArray();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
