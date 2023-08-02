using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Networking.Downloaders.Interfaces;
using NexusMods.Networking.Downloaders.Interfaces.Traits;
using NexusMods.Paths;

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
[JsonName("NexusMods.Networking.Downloaders.Tasks.State.DownloaderState")]
public record DownloaderState : Entity
{
    /// <summary>
    /// Data tied to the type that created this state snapshot.
    /// </summary>
    public required ITypeSpecificState? TypeSpecificData { get; init; }

    /// <summary>
    /// Status of the task associated with this state.
    /// </summary>
    public required DownloadTaskStatus Status { get; init; }

    /// <summary>
    /// Path to the temporary file being downloaded.
    /// </summary>
    /// <remarks>
    ///     In our code this is <see cref="AbsolutePath"/> from <see cref="TemporaryPath"/>; but we can't use that here
    ///     for serialization reasons. Because <see cref="IFileSystem"/> always is <see cref="FileSystem"/> however,
    ///     (more specifically, saved to `/tmp` i.e. `%temp%` on Windows) this is okay to store.
    /// </remarks>
    public required string DownloadPath { get; init; } = "";

    /// <summary>
    /// Friendly name for the suspended download task.
    /// </summary>
    public required string FriendlyName { get; init; } = "";

    /// <summary>
    /// Amount of already downloaded bytes.
    /// </summary>
    public required long DownloadedBytes { get; init; } = 0L;

    /// <summary>
    /// Name of the game the mod will be installed to.
    /// </summary>
    /// <remarks>Provided by <see cref="IHaveGameName"/> trait.</remarks>
    public required string? GameName { get; init; }

    /// <summary>
    /// Unique identifier for the game whose loadouts should be suggested for installation.
    /// </summary>
    /// <remarks>Provided by <see cref="IHaveGameDomain"/> trait.</remarks>
    public required string? GameDomain { get; init; }

    /// <summary>
    /// Size of the file being downloaded in bytes. A value of less than 0 means size is unknown.
    /// </summary>
    /// <remarks>Provided by <see cref="IHaveFileSize"/> trait.</remarks>
    public required long? SizeBytes { get; init; }

    /// <summary>
    /// Version of the mod; can sometimes be arbitrary and not follow SemVer or any standard.
    /// </summary>
    /// <remarks>Provided by <see cref="IHaveDownloadVersion"/> trait.</remarks>
    public required string? Version { get; init; }

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
            DownloadedBytes = item.DownloadedSizeBytes,
            
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
