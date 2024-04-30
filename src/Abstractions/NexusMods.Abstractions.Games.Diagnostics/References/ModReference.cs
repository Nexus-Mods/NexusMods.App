using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

namespace NexusMods.Abstractions.Diagnostics.References;

/// <summary>
/// A reference to a <see cref="Mod"/>.
/// </summary>
[PublicAPI]
public record ModReference : IDataReference<ModId, Mod.Model>
{
    /// <inheritdoc/>
    public required IId DataStoreId { get; init; }

    /// <inheritdoc/>
    public required ModId DataId { get; init; }

    /// <inheritdoc/>
    public Mod.Model? ResolveData(IServiceProvider serviceProvider, IDataStore dataStore)
    {
        throw new NotImplementedException();
        /*
        var loadoutRegistry = serviceProvider.GetRequiredService<ILoadoutRegistry>();
        return loadoutRegistry.Get(DataId);
        */
    }

    /// <inheritdoc/>
    public string ToStringRepresentation(Mod.Model data) => data.Name;
}
