using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Represents persisted data about downloads.
/// </summary>
/// <seealso cref="IDownloadActivity"/>
[PublicAPI]
public partial class PersistedDownloadState : IModelDefinition
{
    private const string Namespace = "NexusMods.Downloads.PersistedDownloadState";

    /// <summary>
    /// Title of the download.
    /// </summary>
    public static readonly StringAttribute Title = new(Namespace, nameof(Title));

    /// <summary>
    /// Status of the download.
    /// </summary>
    public static readonly EnumByteAttribute<PersistedDownloadStatus> Status = new(Namespace, nameof(PersistedDownloadStatus));
}
