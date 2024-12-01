using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Diagnostics.References;

/// <summary>
/// A reference to a <see cref="Mod"/>.
/// </summary>
[PublicAPI]
public record LoadoutItemGroupReference : IDataReference<LoadoutItemGroupId, LoadoutItemGroup.ReadOnly>
{
    /// <inheritdoc/>
    public required TxId TxId { get; init; }

    /// <inheritdoc/>
    public required LoadoutItemGroupId DataId { get; init; }

    /// <inheritdoc/>
    public LoadoutItemGroup.ReadOnly ResolveData(IServiceProvider serviceProvider, IConnection dataStore)
    {
        var db = dataStore.AsOf(TxId);
        return LoadoutItemGroup.Load(db, DataId.Value);
    }

    /// <inheritdoc/>
    public string ToStringRepresentation(LoadoutItemGroup.ReadOnly data) => data.AsLoadoutItem().Name;
}
