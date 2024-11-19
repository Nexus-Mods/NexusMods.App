using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Paths;

namespace NexusMods.App.UI.Pages.CollectionDownload;

public class CollectionDownloadDesignViewModel : APageViewModel<ICollectionDownloadViewModel>, ICollectionDownloadViewModel
{
    public CollectionDownloadTreeDataGridAdapter RequiredDownloadsAdapter { get; } = null!;
    public CollectionDownloadTreeDataGridAdapter OptionalDownloadsAdapter { get; } = null!;

    public CollectionDownloadDesignViewModel() : base(new DesignWindowManager()) { }

    public string Name => "Vanilla+ [Quality of Life]";
    public CollectionSlug Slug { get; } = CollectionSlug.From("tckf0m");
    public RevisionNumber RevisionNumber { get; } = RevisionNumber.From(6);
    public string AuthorName => "Lowtonotolerance";

    public string Summary =>
        "Aims to improves vanilla gameplay while adding minimal additional content. Aims to improves vanilla gameplay while adding minimal additional content. Aims to improves vanilla gameplay while adding minimal additional content. Aims to improves vanilla gameplay while adding minimal additional content.";

    public string Category => "Themed";
    public bool IsAdult => true;
    public int RequiredDownloadsCount => 9;
    public int OptionalDownloadsCount => 2;
    public Bitmap AuthorAvatar => new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/avatar.webp")));
    public ulong EndorsementCount => 248;
    public ulong TotalDownloads => 30_000;
    public Size TotalSize { get; } = Size.From(76_123_456);
    public Percent OverallRating { get; } = Percent.CreateClamped(0.82);
    public Bitmap TileImage { get; } = new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/collection_tile_image.png")));
    public Bitmap BackgroundImage { get; } = new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/header-background.webp")));
    public string CollectionStatusText { get; } = "0 of 9 mods downloaded";
}
