using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.StandardGameLocators.Unknown;

namespace NexusMods.StandardGameLocators;

/// <inheritdoc />
public class GameInstallationConverter : JsonConverter<GameInstallation>
{
    private readonly IGameRegistry _gameRegistry;

    /// <inheritdoc />
    public GameInstallationConverter(IGameRegistry gameRegistry)
    {
        _gameRegistry = gameRegistry;
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

        var foundGame = _gameRegistry.Installations.Values
            .FirstOrDefault(install => install.Store == store && install.Game.Domain == slug && install.Version == version);

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

