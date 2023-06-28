using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.Diagnostics.References;

/// <summary>
/// A reference to a <see cref="Loadout"/>.
/// </summary>
[PublicAPI]
public record LoadoutReference : IDataReference<LoadoutId, Loadout>
{
    /// <inheritdoc/>
    public required LoadoutId DataId { get; init; }
}
