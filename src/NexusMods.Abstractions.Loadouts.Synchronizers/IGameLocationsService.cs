using System.Collections.Frozen;
using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

[PublicAPI]
public record struct IndexFileResult(
    GamePath Path,
    Hash Hash,
    Size Size,
    DateTimeOffset LastModified
);

[PublicAPI]
public record IndexGameResult(
    FrozenDictionary<GamePath, IndexFileResult> NewFiles,
    FrozenDictionary<GamePath, IndexFileResult> ModifiedFiles,
    FrozenSet<GamePath> RemovedFiles
);

[PublicAPI]
public interface IGameLocationsService
{
    Task<IndexGameResult> IndexGame(
        GameInstallation installation,
        FrozenDictionary<GamePath, DiskStateEntry.ReadOnly> previousDiskState,
        ILocationsFilter filter,
        CancellationToken cancellationToken = default
    );

    // Task RemoveEmptyDirectories(GameInstallation installation, CancellationToken cancellationToken = default);
}

[PublicAPI]
public interface ILocationsFilter
{
    bool ShouldFilter(GamePath gamePath);
}

public static class LocationsFilter
{
    public static readonly ILocationsFilter Empty = new EmptyFilter();

    public static ILocationsFilter Create(Func<GamePath, bool> predicate)
    {
        return new PredicateFilter(predicate);
    }

    private class PredicateFilter : ILocationsFilter
    {
        private readonly Func<GamePath, bool> _predicate;

        public PredicateFilter(Func<GamePath, bool> predicate)
        {
            _predicate = predicate;
        }

        public bool ShouldFilter(GamePath gamePath) => _predicate(gamePath);
    }

    private class EmptyFilter : ILocationsFilter
    {
        public bool ShouldFilter(GamePath gamePath) => false;
    }
}
