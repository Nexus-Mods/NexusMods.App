using Avalonia.Media.Imaging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;

namespace NexusMods.App.UI.Pages.LibraryPage.Collections;

public interface ICollectionCardViewModel : IViewModelInterface
{
    public string Name { get; }
    
    public Bitmap Image { get; }
    
    public string Summary { get; }
    
    public string Category { get; }
    
    public int ModCount { get; }
    
    public ulong EndorsementCount { get; }
    
    public ulong DownloadCount { get; }
    
    public Size TotalSize { get; }
    
    public Percent OverallRating { get; }
    
    public string AuthorName { get; }
    
    public Bitmap AuthorAvatar { get; }
}
