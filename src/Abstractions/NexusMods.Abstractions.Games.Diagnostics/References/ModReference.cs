using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Diagnostics.References;

/// <summary>
/// A reference to a <see cref="Mod"/>.
/// </summary>
[PublicAPI]
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
        return db.Get<Mod.ReadOnly>(DataId.Value);
    }

    /// <inheritdoc/>
    public string ToStringRepresentation(Mod.ReadOnly data) => data.Name;
}
