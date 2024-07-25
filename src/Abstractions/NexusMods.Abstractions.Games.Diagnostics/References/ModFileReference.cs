using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.MnemonicDB.Abstractions;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.Diagnostics.References;

/// <summary/>
[PublicAPI]
[Obsolete("To be replaced")]
public record ModFileReference : IDataReference<FileId, File.ReadOnly>
{
    /// <inheritdoc/>
    public required TxId TxId { get; init; }

    /// <inheritdoc/>
    public required FileId DataId { get; init; }

    /// <inheritdoc/>
    public File.ReadOnly ResolveData(IServiceProvider serviceProvider, IConnection conn)
    {
        var db = conn.AsOf(TxId);
        return File.Load(db, DataId.Value);
    }

    /// <inheritdoc/>
    public string ToStringRepresentation(File.ReadOnly data) => data.ToString();
}
