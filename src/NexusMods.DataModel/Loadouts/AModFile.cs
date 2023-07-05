using System.Collections.Immutable;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts.Mods;

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
    public override EntityCategory Category => EntityCategory.ModFiles;

    /// <summary>
    /// Metadata for this file.
    /// </summary>
    public ImmutableList<IMetadata> Metadata { get; init; } = ImmutableList<IMetadata>.Empty;
}
