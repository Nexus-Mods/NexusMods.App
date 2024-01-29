using JetBrains.Annotations;
using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

namespace NexusMods.Abstractions.Diagnostics.References;

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
