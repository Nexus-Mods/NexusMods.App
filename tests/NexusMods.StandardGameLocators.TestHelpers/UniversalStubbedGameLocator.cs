using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Paths;
using NexusMods.Sdk.Games;

namespace NexusMods.StandardGameLocators.TestHelpers;

public class UniversalStubbedGameLocator<TGame> : IGameLocator, IDisposable
    where TGame : IGame
{
    private readonly TemporaryPath _path;
    private readonly TGame _game;

    public LocatorId[] LocatorIds { get; set; } = [LocatorId.From("StubbedGameState.zip")];

    public UniversalStubbedGameLocator(
        IServiceProvider serviceProvider,
        IFileSystem fileSystem,
        TemporaryFileManager fileManager,
        Dictionary<RelativePath, byte[]>? gameFiles = null)
    {
        _path = fileManager.CreateFolder(typeof(TGame).Name);
        _game = serviceProvider.GetRequiredService<TGame>();

        if (gameFiles is null) return;
        foreach (var gameFile in gameFiles)
        {
            var gameFilePath = _path.Path.Combine(gameFile.Key);
            using var stream = fileSystem.CreateFile(gameFilePath);
            stream.Write(gameFile.Value, 0, gameFile.Value.Length);
        }
    }

    public IEnumerable<GameLocatorResult> Locate()
    {
        yield return new GameLocatorResult
        {
            Game = _game,
            Locator = this,
            LocatorIds = [..LocatorIds],
            Store = GameStore.Unknown,
            Path = _path,
            StoreIdentifier = LocatorIds[0].Value,
        };
    }

    public void Dispose()
    {
        _path.Dispose();
    }
}
