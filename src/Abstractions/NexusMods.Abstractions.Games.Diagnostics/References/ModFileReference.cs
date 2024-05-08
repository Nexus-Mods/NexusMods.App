using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.MnemonicDB.Abstractions;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.Diagnostics.References;

/// <summary>
/// A reference to a <see cref="AModFile"/>
/// </summary>
[PublicAPI]
public record ModFileReference : IDataReference<FileId, File.Model>
{
    /// <inheritdoc/>
    public required TxId TxId { get; init; }

    /// <inheritdoc/>
    public required FileId DataId { get; init; }

    /// <inheritdoc/>
    public File.Model? ResolveData(IServiceProvider serviceProvider, IConnection conn)
    {
        var db = conn.AsOf(TxId);
        return db.Get<File.Model>(DataId.Value);
    }

    /// <inheritdoc/>
    public string ToStringRepresentation(File.Model data) => data.ToString();
}
