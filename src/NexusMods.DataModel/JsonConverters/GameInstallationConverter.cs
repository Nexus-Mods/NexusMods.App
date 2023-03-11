using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.Games;

namespace NexusMods.DataModel.JsonConverters;

/// <inheritdoc />
public class GameInstallationConverter : JsonConverter<GameInstallation>
{
    private readonly Dictionary<(GameDomain Slug, Version Version), GameInstallation> _games;

    /// <inheritdoc />
    public GameInstallationConverter(IEnumerable<IGame> games)
    {
        _games = games.SelectMany(g => g.Installations.Select(i => (Slug: g.Domain, Install: i)))
            .ToDictionary(r => (r.Slug, r.Install.Version), r => r.Install);
    }

    /// <inheritdoc />
    public override GameInstallation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array start");

        reader.Read();
        var slug = GameDomain.From(reader.GetString()!);
        reader.Read();
        var version = JsonSerializer.Deserialize<Version>(ref reader, options)!;
        reader.Read();

        if (_games.TryGetValue((slug, version), out var found))
            return found;

        return new UnknownGame(slug, version).Installations.First();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, GameInstallation value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteStringValue(value.Game.Domain.Value);
        JsonSerializer.Serialize(writer, value.Version, options);
        writer.WriteEndArray();
    }
}
