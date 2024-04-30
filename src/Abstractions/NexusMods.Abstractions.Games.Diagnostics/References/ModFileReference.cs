using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.Diagnostics.References;

/// <summary>
/// A reference to a <see cref="AModFile"/>
/// </summary>
[PublicAPI]
public record ModFileReference : IDataReference<FileId, File.Model>
{
    /// <inheritdoc/>
    public required IId DataStoreId { get; init; }

    /// <inheritdoc/>
    public required FileId DataId { get; init; }

    /// <inheritdoc/>
    public File.Model? ResolveData(IServiceProvider serviceProvider, IDataStore dataStore)
    {
        throw new NotImplementedException();
        
        //return dataStore.Get<AModFile>(DataStoreId);
    }

    /// <inheritdoc/>
    public string ToStringRepresentation(File.Model data) => data.ToString();
}
