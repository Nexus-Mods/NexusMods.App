using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators.TestHelpers;

public class UniversalStubbedGameLocator<TGame> : IGameLocator, IDisposable
    where TGame : IGame
{
    private readonly TemporaryPath _path;
    private readonly Version? _version;

    public UniversalStubbedGameLocator(
        IFileSystem fileSystem,
        TemporaryFileManager fileManager,
        Version? version = null,
        Dictionary<RelativePath, byte[]>? gameFiles = null)
    {
        _path = fileManager.CreateFolder(typeof(TGame).Name);
        _version = version;

        if (gameFiles is null) return;
        foreach (var gameFile in gameFiles)
        {
            var gameFilePath = _path.Path.CombineUnchecked(gameFile.Key);
            using var stream = fileSystem.CreateFile(gameFilePath);
            stream.Write(gameFile.Value, 0, gameFile.Value.Length);
        }
    }

    public IEnumerable<GameLocatorResult> Find(IGame game)
    {
        if (game is not TGame)
            yield break;

        yield return new GameLocatorResult(_path, _version ?? new Version(1, 0, 0, 0));
    }

    public void Dispose()
    {
        _path.Dispose();
    }
}
