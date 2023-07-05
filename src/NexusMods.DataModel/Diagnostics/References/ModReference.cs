using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using NexusMods.DataModel.Loadouts.Mods;

namespace NexusMods.DataModel.Diagnostics.References;

/// <summary>
/// A reference to a <see cref="Mod"/>.
/// </summary>
[PublicAPI]
public record ModReference : IDataReference<ModCursor, Mod>
{
    /// <inheritdoc/>
    public required ModCursor DataId { get; init; }

    /// <inheritdoc/>
    public required IId DataStoreId { get; init; }
}
