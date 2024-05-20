using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Networking.Downloaders.Tasks.State;

/// <summary>
/// Additional state for a <see cref="DownloaderState"/> that is completed
/// </summary>
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

    /// <summary>
    /// Model for reading and writing CompletedDownloadStates
    /// </summary>
    /// <param name="tx"></param>
    public class Model(ITransaction tx) : DownloaderState.Model(tx)
    {
        
        /// <summary>
        /// The timestamp the download was completed at
        /// </summary>
        public DateTime CompletedAt
        {
            get => CompletedDateTime.Get(this, default(DateTime));
            set => CompletedDateTime.Add(this, value);
        }
        
        /// <summary>
        /// Whether the download is hidden (clear action) in the UI
        /// </summary>
        public bool IsHidden
        {
            get => Hidden.Get(this, false);
            set => Hidden.Add(this, value);
        }
    }
}
