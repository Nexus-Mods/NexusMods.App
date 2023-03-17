using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators.TestHelpers;

public class UniversalStubbedGameLocator<TGame> : IGameLocator
  where TGame : IGame
{
    private readonly TemporaryPath _path;
    private readonly Version? _version;

    public UniversalStubbedGameLocator(TemporaryFileManager fileManager, Version? version = null)
    {
        _path = fileManager.CreateFolder(typeof(TGame).Name);
        _version = version;
    }
    public IEnumerable<GameLocatorResult> Find(IGame game)
    {
        if (game is not TGame)
            yield break;

        yield return new GameLocatorResult(_path, _version ?? new Version(1, 0, 0, 0));
    }
}
