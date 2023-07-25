using MemoryPack;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders.Tasks.State;

/// <summary>
/// Stores the state of a suspended download.
/// </summary>
/// <typeparam name="TTypeSpecificState">
///     Injected data.
///     Contains type of the state specific to the owner.
///
///     To give an example, <see cref="HttpDownloadTask"/> will have a <see cref="HttpDownloadState"/> injected; which
///     stores an URL. That is specific to the <see cref="HttpDownloadTask"/>, and a task such as <see cref="NxmDownloadTask"/>
///     might not need it since it uses another strategy to start a mod download.
/// </typeparam>
[MemoryPackable(GenerateType.VersionTolerant)]
// ReSharper disable once PartialTypeWithSinglePart
public partial class DownloaderState<TTypeSpecificState>
{
    /// <summary>
    /// Data tied to the type that created this state snapshot.
    /// </summary>
    [MemoryPackOrder(0)]
    public TTypeSpecificState? TypeSpecificData { get; private set; }

    /// <summary>
    /// Path to the temporary file being downloaded.
    /// </summary>
    /// <remarks>
    ///     In our code this is <see cref="AbsolutePath"/> from <see cref="TemporaryPath"/>; but we can't use that here
    ///     for serialization reasons. Because <see cref="IFileSystem"/> always is <see cref="FileSystem"/> however,
    ///     (more specifically, saved to `/tmp` i.e. `%temp%` on Windows) this is okay to store.
    /// </remarks>
    [MemoryPackOrder(1)]
    public string DownloadPath { get; private set; }

    /// <summary>
    /// Friendly name for the suspended download task.
    /// </summary>
    [MemoryPackOrder(2)]
    public string FriendlyName { get; private set; }

    /// <summary>
    /// Name of the game the mod will be installed to.
    /// </summary>
    /// <remarks>Provided by <see cref="IHaveGameName"/> trait.</remarks>
    [MemoryPackOrder(3)]
    public string? GameName { get; private set; }

    /// <summary>
    /// Size of the file being downloaded in bytes. A value of less than 0 means size is unknown.
    /// </summary>
    /// <remarks>Provided by <see cref="IHaveFileSize"/> trait.</remarks>
    [MemoryPackOrder(4)]
    public long? SizeBytes { get; private set; }

    /// <summary>
    /// Version of the mod; can sometimes be arbitrary and not follow SemVer or any standard.
    /// </summary>
    /// <remarks>Provided by <see cref="IHaveDownloadVersion"/> trait.</remarks>
    [MemoryPackOrder(5)]
    public string? Version { get; private set; }

    // Unused, but required for serialization. 
    public DownloaderState()
    {
        FriendlyName = "";
        DownloadPath = "";
    }

    /// <summary>
    /// Deserializes the downloader state.
    /// </summary>
    public static DownloaderState<TTypeSpecificState> Deserialize(byte[] data)
    {
        // Ignored null value because deserializer throws on error and will not return null.
        return MemoryPackSerializer.Deserialize<DownloaderState<TTypeSpecificState>>(data)!;
    }

    /// <summary>
    /// Serializes the downloader state.
    /// </summary>
    /// <typeparam name="TItem">Type of download task whose state is to be serialized.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <param name="downloadLocation">Location of the download in FileSystem.</param>
    public static byte[] Serialize<TItem>(TItem item, string downloadLocation)
        where TItem : IDownloadTask, IHaveTypeSpecificState<TTypeSpecificState> =>
        Serialize(item, item.GetState(), downloadLocation);

    /// <summary>
    /// Serializes the downloader state.
    /// </summary>
    /// <typeparam name="TItem">Type of download task whose state is to be serialized.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <param name="downloadLocation">Location of the download in FileSystem.</param>
    /// <param name="typeSpecificState">State specific to this type being serialized.</param>
    public static byte[] Serialize<TItem>(TItem item, TTypeSpecificState typeSpecificState, string downloadLocation)
        where TItem : IDownloadTask =>
        Serialize(Create(item, typeSpecificState, downloadLocation));

    /// <summary>
    /// Serializes the downloader state.
    /// </summary>
    /// <param name="state">The state to serialize.</param>
    public static byte[] Serialize(DownloaderState<TTypeSpecificState> state) =>
        MemoryPackSerializer.Serialize(state);
    
    /// <summary>
    /// Creates the downloader state given the item to serialize
    /// </summary>
    /// <param name="item">Item to deserialize.</param>
    /// <param name="typeSpecificState">State specific to this type being serialized.</param>
    /// <param name="downloadLocation">Location of the download in FileSystem.</param>
    /// <typeparam name="TItem">Type of download task whose state is to be serialized.</typeparam>
    public static DownloaderState<TTypeSpecificState> Create<TItem>(TItem item, TTypeSpecificState typeSpecificState, string downloadLocation)
        where TItem : IDownloadTask
    {
        var result = new DownloaderState<TTypeSpecificState>();
        result.TypeSpecificData = typeSpecificState;
        result.FriendlyName = item.FriendlyName;
        result.DownloadPath = downloadLocation;
        if (item is IHaveFileSize fileSize)
            result.SizeBytes = fileSize.SizeBytes;

        if (item is IHaveDownloadVersion downloadVersion)
            result.Version = downloadVersion.Version;

        if (item is IHaveGameName gameName)
            result.GameName = gameName.GameName;

        return result;
    }

    #region Generated by Rider
    protected bool Equals(DownloaderState<TTypeSpecificState> other)
    {
        return EqualityComparer<TTypeSpecificState?>.Default.Equals(TypeSpecificData, other.TypeSpecificData) &&
               string.Equals(DownloadPath, other.DownloadPath, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(FriendlyName, other.FriendlyName, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(GameName, other.GameName, StringComparison.OrdinalIgnoreCase) &&
               SizeBytes == other.SizeBytes &&
               string.Equals(Version, other.Version, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DownloaderState<TTypeSpecificState>)obj);
    }
    #endregion
    
    // Download path is a temporary path, thus should be unique.
    public override int GetHashCode() => DownloadPath.GetHashCode(StringComparison.OrdinalIgnoreCase);
}
