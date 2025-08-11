using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.Unknown;

namespace NexusMods.StandardGameLocators.TestHelpers;

public class UniversalStubbedGameLocator<TGame> : IGameLocator, IDisposable
    where TGame : ILocatableGame
{
    private readonly TemporaryPath _path;
    private readonly Version? _version;

    public LocatorId[] LocatorIds { get; set; } = [LocatorId.From("StubbedGameState.zip")];

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
            var gameFilePath = _path.Path.Combine(gameFile.Key);
            using var stream = fileSystem.CreateFile(gameFilePath);
            stream.Write(gameFile.Value, 0, gameFile.Value.Length);
        }
    }

    public IEnumerable<GameLocatorResult> Find(ILocatableGame game)
    {
        if (game is not TGame)
            yield break;

        yield return new GameLocatorResult(
            _path,
            _path.Path.FileSystem,
            OSInformation.Shared,
            GameStore.Unknown,
            new UnknownLocatorResultMetadata(LocatorIds),
            _version ?? new Version(1, 0, 0, 0));
    }

    public void Dispose()
    {
        _path.Dispose();
    }
}
