using GameFinder.Common;
using NexusMods.Paths;
using OneOf;

namespace NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

public class StubbedGameLocator<TGame, TId> : AHandler<TGame, TId>
    where TGame : class, IGame
    where TId : notnull
{
    // ReSharper disable once NotAccessedField.Local
    private readonly Version? _version;
    private readonly TGame _game;

    public StubbedGameLocator(TemporaryFileManager manager,
        Func<TemporaryFileManager, TGame> factory,
        Func<TGame, TId> idSelector,
        Version? version = null)
    {
        IdSelector = idSelector;
        _version = version;
        _game = factory(manager);
    }

    public override Func<TGame, TId> IdSelector { get; }
    public override IEqualityComparer<TId>? IdEqualityComparer => null;

    public override IEnumerable<OneOf<TGame, ErrorMessage>> FindAllGames()
    {
        yield return _game;
    }
}
