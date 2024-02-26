using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

namespace NexusMods.Abstractions.Diagnostics.References;

/// <summary>
/// A reference to a <see cref="Mod"/>.
/// </summary>
[PublicAPI]
public record ModReference : IDataReference<ModCursor, Mod>
{
    /// <inheritdoc/>
    public required IId DataStoreId { get; init; }

    /// <inheritdoc/>
    public required ModCursor DataId { get; init; }

    /// <inheritdoc/>
    public Mod? ResolveData(IServiceProvider serviceProvider, IDataStore dataStore)
    {
        var loadoutRegistry = serviceProvider.GetRequiredService<ILoadoutRegistry>();
        return loadoutRegistry.Get(DataId);
    }

    /// <inheritdoc/>
    public string ToStringRepresentation(Mod data) => data.Name;
}
