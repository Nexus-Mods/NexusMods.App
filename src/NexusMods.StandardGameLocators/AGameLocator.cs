using GameFinder.Common;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

public abstract class AGameLocator<TStore, TRecord, TId, TGame> : IGameLocator
where TStore : AHandler<TRecord, TId> 
where TRecord : class
where TGame : IGame
{
    private readonly ILogger _logger;
    private readonly GameStore _gameStore;
    private readonly AHandler<TRecord,TId> _handler;

    protected AGameLocator(ILogger logger, GameStore gameStore, AHandler<TRecord, TId> handler)
    {
        _logger = logger;
        _gameStore = gameStore;
        _handler = handler;
    }

    public IEnumerable<GameLocatorResult> Find(IGame game)
    {
        if (game is not TGame tg) yield break;
        
        foreach (var id in Ids(tg))
        {
            var found = _handler.FindOneGameById(id, out var errors);
            if (errors.Any() || found == null)
            {
                foreach (var error in errors) 
                    _logger.LogError("While looking for {Game}: {Error}", game, error);
            }
            else
            {
                yield return new GameLocatorResult(Path(found), _gameStore);
            }
        }
    }

    protected abstract IEnumerable<TId> Ids(TGame game);
    protected abstract AbsolutePath Path(TRecord record);
}