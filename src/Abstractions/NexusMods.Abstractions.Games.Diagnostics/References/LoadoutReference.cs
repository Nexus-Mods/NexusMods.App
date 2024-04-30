using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

namespace NexusMods.Abstractions.Diagnostics.References;

/// <summary>
/// A reference to a <see cref="Loadout"/>.
/// </summary>
[PublicAPI]
public record LoadoutReference : IDataReference<LoadoutId, Loadout.Model>
{
    /// <inheritdoc/>
    public required IId DataStoreId { get; init; }

    /// <inheritdoc/>
    public required LoadoutId DataId { get; init; }

    /// <inheritdoc/>
    public Loadout.Model? ResolveData(IServiceProvider serviceProvider, IDataStore dataStore)
    {
        throw new NotImplementedException();
        /*
        var loadoutRegistry = serviceProvider.GetRequiredService<ILoadoutRegistry>();
        return loadoutRegistry.Get(DataId);
        */
    }

    /// <inheritdoc/>
    public string ToStringRepresentation(Loadout.Model data) => data.Name;
}
