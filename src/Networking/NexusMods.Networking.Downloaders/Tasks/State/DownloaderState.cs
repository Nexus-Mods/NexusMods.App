using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Networking.Downloaders.Interfaces;
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
public partial class DownloaderState : IModelDefinition
{
    private const string Namespace = "NexusMods.Networking.Downloaders.Tasks.DownloaderState";
    
    /// <summary>
    /// Status of the task associated with this state.
    /// </summary>
    public static readonly EnumByteAttribute<DownloadTaskStatus> Status = new(Namespace, nameof(Status)) { IsIndexed = true, NoHistory = true };

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
    public static readonly SizeAttribute Downloaded = new(Namespace, nameof(Downloaded)) { IsOptional = true, NoHistory = true };

    /// <summary>
    /// Amount of already downloaded bytes.
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size)) { IsOptional = true };

    /// <summary>
    /// Domain of the game the mod will be installed to.
    /// </summary>
    public static readonly GameDomainAttribute GameDomain = new(Namespace, nameof(GameDomain)) { IsIndexed = true, IsOptional = true};
    
    /// <summary>
    /// Size of the file being downloaded in bytes. A value of less than 0 means size is unknown.
    /// </summary>
    public static readonly SizeAttribute SizeBytes = new(Namespace, nameof(SizeBytes)) { IsOptional = true };

    /// <summary>
    /// Version of the mod; can sometimes be arbitrary and not follow SemVer or any standard.
    /// </summary>
    public static readonly StringAttribute Version = new(Namespace, nameof(Version)) { IsOptional = true };
}
