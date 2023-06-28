using JetBrains.Annotations;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;

namespace NexusMods.DataModel.Diagnostics.References;

/// <summary>
/// A reference to a <see cref="Mod"/>.
/// </summary>
[PublicAPI]
public record ModReference : IDataReference<ModId, Mod>
{
    /// <inheritdoc/>
    public required ModId DataId { get; init; }
}
