using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
using NexusMods.Paths;
using Entity = NexusMods.MnemonicDB.Abstractions.Models.Entity;

namespace NexusMods.Networking.Downloaders.Tasks.State;

/// <summary>
/// Stores the state of a suspended download.
/// </summary>
/// <remarks>
///     <see cref="ITypeSpecificState"/> contains injected data.
///     That is, state specific to the owner.
///
///     To give an example, <see cref="HttpDownloadTask"/> will have a <see cref="HttpDownloadState"/> injected; which
///     stores an URL. That is specific to the <see cref="HttpDownloadTask"/>, and a task such as <see cref="NxmDownloadTask"/>
///     might not need it since it uses another strategy to start a mod download.
/// </remarks>
// ReSharper disable once PartialTypeWithSinglePart
public static class DownloaderState
{
    private const string Namespace = "NexusMods.Networking.Downloaders.Tasks.DownloaderState";
    
    /// <summary>
    /// Data tied to the type that created this state snapshot.
    /// </summary>
    public required ITypeSpecificState? TypeSpecificData { get; init; }

    /// <summary>
    /// Status of the task associated with this state.
    /// </summary>
    public static readonly ByteAttribute Status = new(Namespace, nameof(Status)) { IsIndexed = true, NoHistory = true };

    /// <summary>
    /// Path to the temporary file being downloaded.
    /// </summary>
    /// <remarks>
    ///     In our code this is <see cref="AbsolutePath"/> from <see cref="TemporaryPath"/>; but we can't use that here
    ///     for serialization reasons. Because <see cref="IFileSystem"/> always is <see cref="FileSystem"/> however,
    ///     (more specifically, saved to `/tmp` i.e. `%temp%` on Windows) this is okay to store.
    /// </remarks>
    public static readonly StringAttribute DownloadPath = new(Namespace, nameof(DownloadPath));

    /// <summary>
    /// Friendly name for the suspended download task.
    /// </summary>
    public static readonly StringAttribute FriendlyName = new(Namespace, nameof(FriendlyName));

    /// <summary>
    /// Amount of already downloaded bytes.
    /// </summary>
    public static readonly SizeAttribute Downloaded = new(Namespace, nameof(Downloaded));
    
    /// <summary>
    /// Amount of already downloaded bytes.
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));

    /// <summary>
    /// Domain of the game the mod will be installed to.
    /// </summary>
    /// <remarks>Provided by <see cref="IHaveGameName"/> trait.</remarks>
    public static readonly GameDomainAttribute GameDomain = new(Namespace, nameof(GameDomain)) { IsIndexed = true};
    
    /// <summary>
    /// Size of the file being downloaded in bytes. A value of less than 0 means size is unknown.
    /// </summary>
    /// <remarks>Provided by <see cref="IHaveFileSize"/> trait.</remarks>
    public static readonly SizeAttribute SizeBytes = new(Namespace, nameof(SizeBytes));

    /// <summary>
    /// Version of the mod; can sometimes be arbitrary and not follow SemVer or any standard.
    /// </summary>
    /// <remarks>Provided by <see cref="IHaveDownloadVersion"/> trait.</remarks>
    public static readonly StringAttribute Version = new(Namespace, nameof(Version));


    public class Model(ITransaction tx) : Entity(tx)
    {
        /// <summary>
        /// Status of the download task.
        /// </summary>
        public DownloadTaskStatus Status
        {
            get => (DownloadTaskStatus)DownloaderState.Status.Get(this);
            set => DownloaderState.Status.Add(this, (byte)value);
        }
        
        /// <summary>
        /// The path to the download on disk
        /// </summary>
        public string DownloadPath
        {
            get => DownloaderState.DownloadPath.Get(this);
            set => DownloaderState.DownloadPath.Add(this, value);
        }
        
        /// <summary>
        /// A friendly name for the download
        /// </summary>
        public string FriendlyName
        {
            get => DownloaderState.FriendlyName.Get(this);
            set => DownloaderState.FriendlyName.Add(this, value);
        }
        
        /// <summary>
        /// The size of the file already downloaded
        /// </summary>
        public Size Downloaded
        {
            get => DownloaderState.Downloaded.Get(this);
            set => DownloaderState.Downloaded.Add(this, value);
        }

        /// <summary>
        /// The total size of the file to download
        /// </summary>
        public Size Size
        {
            get => DownloaderState.Size.Get(this);
            set => DownloaderState.Size.Add(this, value);
        }
        
        /// <summary>
        /// The recommended game domain for the download
        /// </summary>
        public GameDomain GameDomain
        {
            get => DownloaderState.GameDomain.Get(this);
            set => DownloaderState.GameDomain.Add(this, value);
        }
        
        /// <summary>
        /// The version of the download
        /// </summary>
        public string Version
        {
            get => DownloaderState.Version.Get(this);
            set => DownloaderState.Version.Add(this, value);
        }
    }

    // Unused, but required for serialization.

    /// <summary>
    /// Creates the downloader state given the item to serialize
    /// </summary>
    /// <param name="item">Item to deserialize.</param>
    /// <param name="typeSpecificState">State specific to this type being serialized.</param>
    /// <param name="downloadLocation">Location of the download in FileSystem.</param>
    /// <typeparam name="TItem">Type of download task whose state is to be serialized.</typeparam>
    public static DownloaderState Create<TItem>(TItem item, ITypeSpecificState typeSpecificState, string downloadLocation)
        where TItem : IDownloadTask
    {
        return new DownloaderState
        {
            TypeSpecificData = typeSpecificState,
            FriendlyName = item.FriendlyName,
            DownloadPath = downloadLocation,
            Status = item.Status,
            Downloaded = item.DownloadedSizeBytes,
            
            // Conditionals
            GameName = item is IHaveGameName gameName ? gameName.GameName : null,
            GameDomain = item is IHaveGameDomain gameDomain ? gameDomain.GameDomain : null,
            SizeBytes = item is IHaveFileSize fileSize ? fileSize.SizeBytes : null,
            Version = item is IHaveDownloadVersion downloadVersion ? downloadVersion.Version : null
        };
    }

    // Download path is a temporary path, thus should be unique.
    // ReSharper disable once NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => DownloadPath.GetHashCode(StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override EntityCategory Category => EntityCategory.DownloadStates;
}
