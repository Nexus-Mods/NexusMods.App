using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.Games;

namespace NexusMods.DataModel.JsonConverters;

/// <inheritdoc />
public class GameInstallationConverter : JsonConverter<GameInstallation>
{
    private readonly ILookup<GameDomain, IGame> _games;

    /// <inheritdoc />
    public GameInstallationConverter(IEnumerable<IGame> games)
    {
        _games = games.ToLookup(d => d.Domain);
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

        var storeString = reader.GetString();
        var store = storeString is null ? GameStore.Unknown : GameStore.From(storeString);

        reader.Read();

        var foundGame = _games[slug]
            .SelectMany(g => g.Installations)
            .FirstOrDefault(install => install.Version == version && install.Store == store);

        return foundGame ?? new UnknownGame(slug, version).Installations.First();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, GameInstallation value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteStringValue(value.Game.Domain.Value);
        JsonSerializer.Serialize(writer, value.Version, options);
        writer.WriteStringValue(value.Store.Value);
        writer.WriteEndArray();
    }
}
