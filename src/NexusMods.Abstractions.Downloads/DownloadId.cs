using JetBrains.Annotations;
using TransparentValueObjects;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Unique identifier for a download that abstracts over JobId.
/// This allows the service to handle both active downloads (with JobId) and completed downloads.
/// </summary>
[PublicAPI]
[ValueObject<uint>]
public readonly partial struct DownloadId : IAugmentWith<DefaultValueAugment, JsonAugment>
{
    // Static counter for generating unique IDs
    private static uint _nextId = 0;
    
    /// <inheritdoc/>
    public static DownloadId DefaultValue { get; } = From(0);
    
    /// <summary>
    /// Generates a new unique DownloadId using atomic increment.
    /// </summary>
    public static DownloadId New()
    {
        var id = Interlocked.Increment(ref _nextId);
        return From(id);
    }
}