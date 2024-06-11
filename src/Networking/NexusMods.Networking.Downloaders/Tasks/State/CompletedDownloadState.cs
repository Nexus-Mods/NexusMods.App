using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Networking.Downloaders.Tasks.State;

/// <summary>
/// Additional state for a <see cref="DownloaderState"/> that is completed
/// </summary>
[Include<DownloaderState>]
public static class CompletedDownloadState
{
    private const string Namespace = "NexusMods.Networking.Downloaders.Tasks.DownloaderState";
    
    /// <summary>
    /// The timestamp the download was completed at
    /// </summary>
    public static readonly DateTimeAttribute CompletedDateTime= new(Namespace, nameof(CompletedDateTime));
    
    /// <summary>
    /// Whether the download is hidden (clear action) in the UI
    /// </summary>
    public static readonly BooleanAttribute Hidden = new(Namespace, nameof(Hidden));
}
