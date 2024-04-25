using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.Networking.Downloaders.Tasks.State;

public static class NxmDownloadState
{
    private const string Namespace = "NexusMods.Networking.Downloaders.Tasks.State.NxmDownloadState";


    /// <summary>
    /// The mod id of the download task
    /// </summary>
    public static readonly ModIdAttribute ModId = new(Namespace, nameof(ModId)) { IsIndexed = true };
    
    /// <summary>
    /// The file id of the download task
    /// </summary>
    public static readonly FileIdAttribute FileId = new(Namespace, nameof(FileId)) { IsIndexed = true };
    
    /// <summary>
    /// The game domain of the download task
    /// </summary>
    public static readonly StringAttribute Game = new(Namespace, nameof(Game)) { IsIndexed = true };
    
    /// <summary>
    /// The expiry date of the download key
    /// </summary>
    public static readonly TimestampAttribute ValidUntil = new(Namespace, nameof(ValidUntil));

    /// <summary>
    /// The NXM key of the download task, used for free users and clicking "Download with manager"
    /// on the website
    /// </summary>
    public static readonly StringAttribute NxmKey = new(Namespace, nameof(NXMKey));


    /// <summary>
    /// Model for reading and writing NXMDownloadStates
    /// </summary>
    public class Model(ITransaction tx) : DownloaderState.Model(tx)
    {

        /// <summary>
        /// ModId of the download task
        /// </summary>
        public ModId ModId
        {
            get => NxmDownloadState.ModId.Get(this);
            set => NxmDownloadState.ModId.Add(this, value);
        }
        
        /// <summary>
        /// FileId of the download task
        /// </summary>
        public FileId FileId
        {
            get => NxmDownloadState.FileId.Get(this);
            set => NxmDownloadState.FileId.Add(this, value);
        }
        
        /// <summary>
        /// Game domain of the download task
        /// </summary>
        public string Game
        {
            get => NxmDownloadState.Game.Get(this);
            set => NxmDownloadState.Game.Add(this, value);
        }
        
        /// <summary>
        /// Expiry date of the download key
        /// </summary>
        public DateTime ValidUntil
        {
            get => NxmDownloadState.ValidUntil.Get(this);
            set => NxmDownloadState.ValidUntil.Add(this, value);
        }
        
        /// <summary>
        /// The NXM key of the download task, used for free users and clicking "Download with manager"
        /// </summary>
        public string NxmKey
        {
            get => NxmDownloadState.NxmKey.Get(this);
            set => NxmDownloadState.NxmKey.Add(this, value);
        }
    }

}
