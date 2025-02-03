using Avalonia.Media.Imaging;
using DynamicData.Kernel;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Collections;
using NexusMods.Paths;
using R3;

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

    IMarkdownRendererViewModel? InstructionsRenderer { get; }
    ModInstructions[] RequiredModsInstructions { get; }
    ModInstructions[] OptionalModsInstructions { get; }

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

    int CountDownloadedRequiredItems { get; }

    /// <summary>
    /// The number of optional downloads in the collection
    /// </summary>
    int OptionalDownloadsCount { get; }

    int CountDownloadedOptionalItems { get; }

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

    bool CanDownloadAutomatically { get; }

    BindableReactiveProperty<bool> IsInstalled { get; }
    BindableReactiveProperty<bool> IsInstalling { get; }
    BindableReactiveProperty<bool> IsDownloading { get; }
    BindableReactiveProperty<bool> IsUpdateAvailable { get; }
    BindableReactiveProperty<bool> HasInstalledAllOptionalItems { get; }
    BindableReactiveProperty<Optional<RevisionNumber>> NewestRevisionNumber { get; }

    ReactiveCommand<NavigationInformation> CommandViewCollection { get; }
    ReactiveCommand<Unit> CommandDownloadRequiredItems { get; }
    ReactiveCommand<Unit> CommandInstallRequiredItems { get; }

    ReactiveCommand<Unit> CommandDownloadOptionalItems { get; }
    ReactiveCommand<Unit> CommandInstallOptionalItems { get; }

    ReactiveCommand<Unit> CommandUpdateCollection { get; }

    ReactiveCommand<Unit> CommandViewOnNexusMods { get; }
    ReactiveCommand<Unit> CommandOpenJsonFile { get; }
    ReactiveCommand<Unit> CommandDeleteAllDownloads { get; }
    ReactiveCommand<Unit> CommandDeleteCollectionRevision { get; }
}

public record ModInstructions(string ModName, string Instructions, CollectionDownloader.ItemType ItemType);
