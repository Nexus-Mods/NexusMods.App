using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Represents persisted data about.
/// </summary>
[PublicAPI]
[Include<PersistedJobState>]
public partial class PersistedDownloadState : IModelDefinition
{
    private const string Namespace = "NexusMods.Downloads.PersistedDownloadState";

    public static readonly MarkerAttribute Marker = new(Namespace, nameof(Marker));
}
