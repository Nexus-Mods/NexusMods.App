using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace NexusMods.Sdk.Games;

/// <summary>
/// Represents all game locations as <see cref="GameLocationDescriptor"/> for an installation.
/// </summary>
[PublicAPI]
public readonly struct GameLocations : IReadOnlyDictionary<LocationId, GameLocationDescriptor>, IEquatable<GameLocations>
{
    private readonly FrozenDictionary<LocationId, GameLocationDescriptor> _locations;

    private GameLocations(FrozenDictionary<LocationId, GameLocationDescriptor> locations)
    {
        _locations = locations;
    }

    /// <summary>
    /// Parses the resolved locations into <see cref="GameLocationDescriptor"/>.
    /// </summary>
    public static GameLocations Create(ImmutableDictionary<LocationId, AbsolutePath> resolvedLocations)
    {
        var results = new Dictionary<LocationId, GameLocationDescriptor>(capacity: resolvedLocations.Count);

        var nestedLocations = new List<LocationId>(capacity: resolvedLocations.Count);
        foreach (var (currentLocation, currentPath) in resolvedLocations)
        {
            var topLevelParent = Optional<(LocationId, AbsolutePath)>.None;
            nestedLocations.Clear();

            foreach (var (otherLocation, otherPath) in resolvedLocations)
            {
                if (otherLocation == currentLocation) continue;

                if (otherPath.InFolder(currentPath))
                {
                    nestedLocations.Add(otherLocation);
                } else if (currentPath.InFolder(otherPath))
                {
                    if (!topLevelParent.HasValue) topLevelParent = (otherLocation, otherPath);
                    else
                    {
                        topLevelParent = topLevelParent.Value.Item2.InFolder(otherPath) ? (otherLocation, otherPath) : topLevelParent;
                    }
                }
            }

            var descriptor = new GameLocationDescriptor(
                currentLocation,
                currentPath,
                nestedLocations: [..nestedLocations],
                topLevelParent: topLevelParent.Convert(tuple => tuple.Item1)
            );

            results.Add(currentLocation, descriptor);
        }

        return new GameLocations(results.ToFrozenDictionary());
    }

    public IEnumerable<KeyValuePair<LocationId, AbsolutePath>> GetTopLevelLocations()
    {
        // NOTE(erri120): Ordering is not guaranteed when using a dictionary.
        // TODO: use explicit location priority ordering 
        return _locations
            .Where(kv => kv.Value.IsTopLevel)
            .GroupBy(kv => kv.Value.Path)
            .Select(grouping => grouping.First())
            .Select(kv => new KeyValuePair<LocationId, AbsolutePath>(kv.Key, kv.Value.Path));
    }

    public GamePath ToGamePath(AbsolutePath path)
    {
        return _locations
            .Where(kv => path.InFolder(kv.Value.Path))
            .Select(kv => new GamePath(kv.Key, path.RelativeTo(kv.Value.Path)))
            .MinBy(gamePath => gamePath.Path.Path.Length);
    }

    public AbsolutePath ToAbsolutePath(GamePath gamePath) => _locations[gamePath.LocationId].Path.Combine(gamePath.Path);

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public FrozenDictionary<LocationId, GameLocationDescriptor>.Enumerator GetEnumerator() => _locations.GetEnumerator();
    /// <inheritdoc/>
    IEnumerator<KeyValuePair<LocationId, GameLocationDescriptor>> IEnumerable<KeyValuePair<LocationId, GameLocationDescriptor>>.GetEnumerator() => _locations.GetEnumerator();
    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => _locations.GetEnumerator();
    /// <inheritdoc/>
    public int Count => _locations.Count;
    /// <inheritdoc/>
    public bool ContainsKey(LocationId key) => _locations.ContainsKey(key);
    /// <inheritdoc/>
    public bool TryGetValue(LocationId key, [MaybeNullWhen(false)] out GameLocationDescriptor value) => _locations.TryGetValue(key, out value);
    /// <inheritdoc/>
    public GameLocationDescriptor this[LocationId key] => _locations[key];
    /// <inheritdoc cref="IReadOnlyDictionary{TKey,TValue}.Keys"/>
    public ImmutableArray<LocationId> Keys => _locations.Keys;
    /// <inheritdoc/>
    IEnumerable<LocationId> IReadOnlyDictionary<LocationId, GameLocationDescriptor>.Keys => _locations.Keys;
    /// <inheritdoc cref="IReadOnlyDictionary{TKey,TValue}.Values"/>
    public ImmutableArray<GameLocationDescriptor> Values => _locations.Values;
    /// <inheritdoc/>
    IEnumerable<GameLocationDescriptor> IReadOnlyDictionary<LocationId, GameLocationDescriptor>.Values => _locations.Values;

    public bool Equals(GameLocations other) => _locations.Equals(other._locations);
    public override bool Equals(object? obj) => obj is GameLocations other && Equals(other);
    public override int GetHashCode() => _locations.GetHashCode();
}
