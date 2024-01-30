using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

namespace NexusMods.Abstractions.Diagnostics.References;

/// <summary>
/// A reference to a <see cref="AModFile"/>
/// </summary>
[PublicAPI]
public record ModFileReference : IDataReference<ModFileId, AModFile>
{
    /// <inheritdoc/>
    public required ModFileId DataId { get; init; }

    /// <inheritdoc/>
    public required IId DataStoreId { get; init; }
}
