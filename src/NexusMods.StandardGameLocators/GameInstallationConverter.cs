using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.StandardGameLocators.Unknown;

namespace NexusMods.StandardGameLocators;

/// <inheritdoc />
public class GameInstallationConverter : JsonConverter<GameInstallation>
{
    private readonly Lazy<IGameRegistry> _gameRegistry;

    /// <inheritdoc />
    public GameInstallationConverter(IServiceProvider provider)
    {
        _gameRegistry = new Lazy<IGameRegistry>(() => (IGameRegistry) provider.GetService(typeof(IGameRegistry))!);
    }

    /// <inheritdoc />
    public override GameInstallation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var gameId = reader.GetUInt64();
        if (_gameRegistry.Value.Installations.TryGetValue(EntityId.From(gameId), out var installation))
        {
            return installation;
        }
        throw new InvalidOperationException($"Game installation with ID {gameId} not found.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, GameInstallation value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.GameMetadataId.Value);
    }
}

