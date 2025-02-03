using Avalonia.Media.Imaging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.Paths;

namespace NexusMods.App.UI.Pages.LibraryPage.Collections;

/// <summary>
/// Collection card view model.
/// </summary>
public interface ICollectionCardViewModel : IViewModelInterface
{
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
    /// The tile image of the collection.
    /// </summary>
    Bitmap? Image { get; }

    /// <summary>
    /// Number of files/mods to download.
    /// </summary>
    int NumDownloads { get; }
    
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
    /// Command to open the download page.
    /// </summary>
    R3.ReactiveCommand<NavigationInformation> OpenCollectionDownloadPageCommand { get; }
}
