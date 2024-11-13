using Avalonia.Media.Imaging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.Paths;

namespace NexusMods.App.UI.Pages.LibraryPage.Collections;

/// <summary>
/// View model for a collection card.
/// </summary>
public interface ICollectionCardViewModel : IViewModelInterface
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// The tile image of the collection.
    /// </summary>
    public Bitmap? Image { get; }
    
    /// <summary>
    /// Summary of the collection.
    /// </summary>
    public string Summary { get; }
    
    /// <summary>
    /// The website category of the collection.
    /// </summary>
    public string Category { get; }
    
    /// <summary>
    /// Number of mods in the collection.
    /// </summary>
    public int ModCount { get; }
    
    /// <summary>
    /// Endorsement count of the collection.
    /// </summary>
    public ulong EndorsementCount { get; }
    
    /// <summary>
    /// Number of downloads of the collection.
    /// </summary>
    public ulong DownloadCount { get; }
    
    /// <summary>
    /// Total size of the collection (including all mods).
    /// </summary>
    public Size TotalSize { get; }
    
    /// <summary>
    /// The overall rating of the collection.
    /// </summary>
    public Percent OverallRating { get; }
    
    /// <summary>
    /// The name of the author of the collection.
    /// </summary>
    public string AuthorName { get; }
    
    /// <summary>
    /// The author's avatar.
    /// </summary>
    public Bitmap? AuthorAvatar { get; }

    R3.ReactiveCommand<NavigationInformation> OpenCollectionDownloadPageCommand { get; }
}
