using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.Paths;

namespace NexusMods.App.UI.Pages.LibraryPage.Collections;

public class CollectionCardDesignViewModel : AViewModel<ICollectionCardViewModel>, ICollectionCardViewModel
{
    public string Name => "Vanilla+ [Quality of Life]";
    public Bitmap Image => new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/collection_tile_image.png")));
    public string Summary => "1.6.14 The story of Stardew Valley expands outside of Pelican Town with this expanded collection designed to stay true to the original game. Created with co-op in mind, perfect for experienced solo-players. Easy install, includes configuration.";
    public string Category => "Themed";
    public int NumDownloads => 9;
    public RevisionNumber RevisionNumber => RevisionNumber.From(123);
    public ulong EndorsementCount => 248;
    public ulong TotalDownloads => 30_000;
    public Size TotalSize => Size.From(907 * 1024 * 1024);
    public Percent OverallRating => Percent.CreateClamped(0.54);
    public bool IsAdult => true;
    public string AuthorName => "FantasyAuthor";
    public Bitmap AuthorAvatar => new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/avatar.webp")));
    public R3.ReactiveCommand<NavigationInformation> OpenCollectionDownloadPageCommand { get; } = new(canExecuteSource: R3.Observable.Return(true), initialCanExecute: true);
}
