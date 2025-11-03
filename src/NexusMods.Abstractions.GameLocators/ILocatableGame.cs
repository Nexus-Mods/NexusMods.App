using NexusMods.Sdk.Games;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Base interface for a game which can be located by a <see cref="IGameLocator"/>.
/// </summary>
public interface ILocatableGame : IGameData;
