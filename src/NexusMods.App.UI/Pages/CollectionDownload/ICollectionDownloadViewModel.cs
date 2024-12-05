using Avalonia.Media.Imaging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Paths;

namespace NexusMods.App.UI.Pages.CollectionDownload;

public interface ICollectionDownloadViewModel : IPageViewModelInterface
{
    CollectionDownloadTreeDataGridAdapter TreeDataGridAdapter { get; }

    /// <inheritdoc cref="CollectionMetadata.Name"/>
    string Name { get; }

    /// <inheritdoc cref="CollectionMetadata.Summary"/>
    string Summary { get; }

    /// <inheritdoc cref="CollectionMetadata.Category"/>
    string Category { get; }

    /// <inheritdoc cref="CollectionMetadata.Endorsements"/>
    ulong EndorsementCount { get; }

    /// <inheritdoc cref="CollectionMetadata.TotalDownloads"/>
    ulong TotalDownloads { get; }

    /// <inheritdoc cref="CollectionRevisionMetadata.TotalSize"/>
    Size TotalSize { get; }

    /// <inheritdoc cref="CollectionRevisionMetadata.OverallRating"/>
    Percent OverallRating { get; }

    /// <inheritdoc cref="CollectionRevisionMetadata.IsAdult"/>
    bool IsAdult { get; }

    /// <summary>
    /// The collection's revision number
    /// </summary>
    RevisionNumber RevisionNumber { get; }

    /// <summary>
    /// The name of the author of the collection.
    /// </summary>
    string AuthorName { get; }

    /// <summary>
    /// The author's avatar.
    /// </summary>
    Bitmap? AuthorAvatar { get; }

    /// <summary>
    /// Download count.
    /// </summary>
    int DownloadCount => RequiredDownloadsCount + OptionalDownloadsCount;

    /// <summary>
    /// The number of required downloads in the collection
    /// </summary>
    int RequiredDownloadsCount { get; }

    /// <summary>
    /// The number of optional downloads in the collection
    /// </summary>
    int OptionalDownloadsCount { get; }

    /// <summary>
    /// The small tile image of the collection
    /// </summary>
    Bitmap? TileImage { get; }

    /// <summary>
    /// The background banner image of the collection
    /// </summary>
    Bitmap? BackgroundImage { get; }

    /// <summary>
    /// Collection status text.
    /// </summary>
    string CollectionStatusText { get; }

    /// <summary>
    /// Command to download all downloads.
    /// </summary>
    R3.ReactiveCommand<R3.Unit> DownloadAllCommand { get; }

    /// <summary>
    /// Command to install the collection.
    /// </summary>
    R3.ReactiveCommand<R3.Unit> InstallCollectionCommand { get; }

    R3.ReactiveCommand<R3.Unit> CommandDeleteCollection { get; }

    R3.ReactiveCommand<R3.Unit> CommandDeleteAllDownloads { get; }

    R3.ReactiveCommand<R3.Unit> CommandViewOnNexusMods { get; }
}
