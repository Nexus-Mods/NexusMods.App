using NexusMods.DataModel.Games;

namespace NexusMods.DataModel.TransformerHooks;

/// <summary>
/// Specifies that this transformer hook is only executed on events for the specified games.
/// </summary>
public interface IGameFilteringHook
{
    /// <summary>
    /// Only execute this hook on events for these game domains, if empty then execute on all game domains.
    /// </summary>
    IEnumerable<GameDomain> GameDomains { get; }
}
