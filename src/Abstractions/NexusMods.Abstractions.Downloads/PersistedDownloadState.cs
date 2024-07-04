using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Represents a download.
/// </summary>
[PublicAPI]
public partial class PersistedDownloadState : IModelDefinition
{
    private const string Namespace = "NexusMods.Downloads.PersistedDownloadState";

    /// <summary>
    /// Status of the download.
    /// </summary>
    public static readonly EnumByteAttribute<PersistedDownloadStatus> Status = new(Namespace, nameof(PersistedDownloadStatus));
}
