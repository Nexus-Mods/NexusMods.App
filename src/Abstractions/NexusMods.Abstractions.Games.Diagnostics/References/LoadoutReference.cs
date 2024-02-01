using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

namespace NexusMods.Abstractions.Diagnostics.References;

/// <summary>
/// A reference to a <see cref="Loadout"/>.
/// </summary>
[PublicAPI]
public record LoadoutReference : IDataReference<LoadoutId, Loadout>
{
    /// <inheritdoc/>
    public required LoadoutId DataId { get; init; }

    /// <inheritdoc/>
    public required IId DataStoreId { get; init; }
}
