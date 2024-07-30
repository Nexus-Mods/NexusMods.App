using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Diagnostics.References;

/// <summary>
/// A reference to a <see cref="Mod"/>.
/// </summary>
[PublicAPI]
[Obsolete($"To be replaced by {nameof(LoadoutItemGroupReference)}")]
public record ModReference : IDataReference<ModId, Mod.ReadOnly>
{
    /// <inheritdoc/>
    public required TxId TxId { get; init; }

    /// <inheritdoc/>
    public required ModId DataId { get; init; }

    /// <inheritdoc/>
    public Mod.ReadOnly ResolveData(IServiceProvider serviceProvider, IConnection dataStore)
    {
        var db = dataStore.AsOf(TxId);
        return Mod.Load(db, DataId.Value);
    }

    /// <inheritdoc/>
    public string ToStringRepresentation(Mod.ReadOnly data) => data.Name;
}
