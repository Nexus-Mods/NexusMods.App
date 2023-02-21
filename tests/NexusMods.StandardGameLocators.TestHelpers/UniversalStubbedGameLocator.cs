using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators.TestHelpers;

public class UniversalStubbedGameLocator<TGame> : IGameLocator
  where TGame : IGame
{
    private readonly TemporaryFileManager _tempFileManager;
    private readonly TemporaryPath _path;
    private readonly Version? _version;

    public UniversalStubbedGameLocator(TemporaryFileManager fileManager, Version? version = null)
    {
        _tempFileManager = fileManager;
        _path = _tempFileManager.CreateFolder(typeof(TGame).Name);
        _version = version;
    }
    public IEnumerable<GameLocatorResult> Find(IGame game)
    {
        if (game is not TGame tg)
            yield break;
        
        yield return new GameLocatorResult(_path, GameStore.Unknown, _version ?? new Version(1, 0, 0, 0));
    }
}