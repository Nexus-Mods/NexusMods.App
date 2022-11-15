using NexusMods.Paths;

namespace NexusMods.Interfaces.Components;

public interface IGameLocator
{
    public IEnumerable<GameLocatorResult> Find(IGame game);
}