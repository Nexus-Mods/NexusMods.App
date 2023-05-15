using System.Collections.Immutable;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// Represents an individual file which belongs to a <see cref="Mod"/>.
/// </summary>
public abstract record AModFile : Entity
{
    /// <summary>
    /// Unique identifier for this mod file.
    /// </summary>
    public required ModFileId Id { get; init; }

    /// <inheritdoc />
    public override EntityCategory Category => EntityCategory.Loadouts;

    /// <summary>
    /// The location relative to the game folder where the file is deployed.
    /// </summary>
    public required GamePath To { get; init; }

    /// <summary>
    /// Metadata for this file.
    /// </summary>
    public ImmutableHashSet<IModFileMetadata> Metadata { get; init; } = ImmutableHashSet<IModFileMetadata>.Empty;
}
