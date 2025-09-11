using NexusMods.Abstractions.GameLocators;

namespace NexusMods.Games.CreationEngine.Abstractions;

public interface ICreationEngineGame
{
    public GamePath PluginsFile { get; }
}
