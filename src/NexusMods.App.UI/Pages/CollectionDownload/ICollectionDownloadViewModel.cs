using Avalonia.Media.Imaging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Paths;

namespace NexusMods.App.UI.Pages.CollectionDownload;

public interface ICollectionDownloadViewModel : IPageViewModelInterface
{
    /// <summary>
    /// Name of the collection
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// The collection's slug
    /// </summary>
    public CollectionSlug Slug { get; }
    
    /// <summary>
    /// The collection's revision number
    /// </summary>
    public RevisionNumber RevisionNumber { get; }
    
    /// <summary>
    /// Name of the author of the collection
    /// </summary>
    public string AuthorName { get; }
    
    /// <summary>
    /// The summary (short description) of the collection
    /// </summary>
    public string Summary { get; }
    
    /// <summary>
    /// Total number of mods in the collection
    /// </summary>
    public int ModCount { get; }
    
    /// <summary>
    /// The number of required mods in the collection
    /// </summary>
    public int RequiredModCount { get; }
    
    /// <summary>
    /// The number of optional mods in the collection
    /// </summary>
    public int OptionalModCount { get; }
    
    /// <summary>
    /// The number of endorsements the collection has
    /// </summary>
    public int EndorsementCount { get; }
    
    /// <summary>
    /// The number of downloads the collection has
    /// </summary>
    public int DownloadCount { get; }
    
    /// <summary>
    /// The size of the collection including all downloads and the collection file iteself
    /// </summary>
    public Size TotalSize { get; }
    
    /// <summary>
    /// The overall approval rating of the collection
    /// </summary>
    public Percent OverallRating { get; }
    
    /// <summary>
    /// The small tileable image of the collection
    /// </summary>
    public Bitmap TileImage { get; }
    
    /// <summary>
    /// The background banner image of the collection
    /// </summary>
    public Bitmap BackgroundImage { get; }
    
    /// <summary>
    /// A text representation of the collection's status, such as "Downloading", "Installing", "Ready to Play", etc.
    /// </summary>
    public string CollectionStatusText { get; }
    
    /// <summary>
    /// The tree data grid adapter for the required mods
    /// </summary>
    public LibraryTreeDataGridAdapter RequiredModsAdapter { get; }
    
    /// <summary>
    /// The tree data grid adapter for the optional mods
    /// </summary>
    public LibraryTreeDataGridAdapter OptionalModsAdapter { get; }
}
