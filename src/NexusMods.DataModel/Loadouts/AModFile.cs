using System.Collections.Immutable;
using NexusMods.DataModel.Abstractions;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts;

public abstract record AModFile : Entity
{
    public required ModFileId Id { get; init; }
    public override EntityCategory Category => EntityCategory.Loadouts;
    public required GamePath To { get; init; }
    public ImmutableHashSet<IModFileMetadata> Metadata { get; init; } = ImmutableHashSet<IModFileMetadata>.Empty;
}
