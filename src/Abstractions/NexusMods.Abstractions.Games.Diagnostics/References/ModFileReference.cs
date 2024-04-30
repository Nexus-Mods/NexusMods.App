using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using File = System.IO.File;

namespace NexusMods.Abstractions.Diagnostics.References;

/// <summary>
/// A reference to a <see cref="AModFile"/>
/// </summary>
[PublicAPI]
public record ModFileReference : IDataReference<ModFileId, File.Model>
{
    /// <inheritdoc/>
    public required IId DataStoreId { get; init; }

    /// <inheritdoc/>
    public required ModFileId DataId { get; init; }

    /// <inheritdoc/>
    public AModFile? ResolveData(IServiceProvider serviceProvider, IDataStore dataStore)
    {
        return dataStore.Get<AModFile>(DataStoreId);
    }

    /// <inheritdoc/>
    public string ToStringRepresentation(AModFile data) => data.ToString();
}
