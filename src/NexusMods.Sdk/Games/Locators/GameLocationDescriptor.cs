using System.Collections.Immutable;
using DynamicData.Kernel;
using NexusMods.Paths;

namespace NexusMods.Sdk.Games;

public class GameLocationDescriptor
{
    public LocationId LocationId { get; }
    public AbsolutePath Path { get; }
    public ImmutableArray<LocationId> NestedLocations { get; }
    public Optional<LocationId> TopLevelParent { get; }

    public bool IsTopLevel => !TopLevelParent.HasValue;

    public GameLocationDescriptor(LocationId locatorId, AbsolutePath path, ImmutableArray<LocationId> nestedLocations, Optional<LocationId> topLevelParent)
    {
        LocationId = locatorId;
        Path = path;
        NestedLocations = nestedLocations;
        TopLevelParent = topLevelParent;
    }
}
