using GameFinder.Common;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

public class StubbedGameLocator<TGame, TId> : AHandler<TGame, TId>
    where TGame : class where TId : notnull
{
    // ReSharper disable once NotAccessedField.Local
    private readonly Version? _version;
    private readonly TemporaryFileManager _manager;
    private readonly Func<TemporaryFileManager,TGame> _factory;
    private readonly Func<TGame, TId> _idSelector;
    private readonly TGame _game;

    public StubbedGameLocator(TemporaryFileManager manager,
        Func<TemporaryFileManager, TGame> factory,
        Func<TGame, TId> idSelector,
        Version? version = null)
    {
        _manager = manager;
        _factory = factory;
        _idSelector = idSelector;
        _version = version;
        _game = factory(_manager);
    }
    public override IEnumerable<Result<TGame>> FindAllGames()
    {
        return new[]
        {
            Result.FromGame(_game)
        };
    }

    public override Dictionary<TId, TGame> FindAllGamesById(out string[] errors)
    {
        errors = Array.Empty<string>();
        return FindAllGames().ToDictionary(g => _idSelector(g.Game!), v => v.Game)!;
    }
}
