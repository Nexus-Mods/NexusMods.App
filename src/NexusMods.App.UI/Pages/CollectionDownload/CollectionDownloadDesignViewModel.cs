using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DynamicData.Kernel;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Collections;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Pages.CollectionDownload;

public class CollectionDownloadDesignViewModel : APageViewModel<ICollectionDownloadViewModel>, ICollectionDownloadViewModel
{
    public CollectionDownloadTreeDataGridAdapter TreeDataGridAdapter { get; } = null!;

    public CollectionDownloadDesignViewModel() : base(new DesignWindowManager()) { }

    public string Name => "Vanilla+ [Quality of Life]";
    public CollectionSlug Slug { get; } = CollectionSlug.From("tckf0m");
    public RevisionNumber RevisionNumber { get; } = RevisionNumber.From(6);
    public string AuthorName => "Lowtonotolerance";

    public string Summary =>
        "1.6.14 The story of Stardew Valley expands outside of Pelican Town with this expanded collection designed to stay true to the original game. Created with co-op in mind, perfect for experienced solo-players. Easy install, includes configuration.";

    public string Category => "Themed";
    public bool IsAdult => true;
    public int RequiredDownloadsCount => 9;
    public int OptionalDownloadsCount => 2;
    public Bitmap AuthorAvatar => new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/avatar.webp")));
    public ulong EndorsementCount => 248;
    public ulong TotalDownloads => 30_000;
    public Size TotalSize { get; } = Size.From(76_123_456);
    public Percent OverallRating { get; } = Percent.CreateClamped(0);
    public Bitmap TileImage { get; } = new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/collection_tile_image.png")));
    public Bitmap BackgroundImage { get; } = new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/header-background.webp")));
    public string CollectionStatusText { get; } = "0 of 9 mods downloaded";
    public IMarkdownRendererViewModel? InstructionsRenderer { get; } = new MarkdownRendererDesignViewModel("This is a collection of mods that make the game better. Please read the instructions carefully.");
    public ModInstructions[] RequiredModsInstructions { get; set; } = [ 
        new ModInstructions("Mod 1", "This required mod needs special instructions", CollectionDownloader.ItemType.Required),
        new ModInstructions("Mod 4", "This required mod also needs some special instructions. This does need to be really long so that we can test that it wraps correctly.", CollectionDownloader.ItemType.Required) ];
    public ModInstructions[] OptionalModsInstructions { get; set; } = [ 
        new ModInstructions("Mod 2", "This optional mod might need something special to happen", CollectionDownloader.ItemType.Optional),
        new ModInstructions("Mod 3", "This ANOTHER optional mod needs some love", CollectionDownloader.ItemType.Optional)];

    public int CountDownloadedOptionalItems { get; } = 1;
    public int CountDownloadedRequiredItems { get; } = 1;
    public bool CanDownloadAutomatically { get; } = false;

    public BindableReactiveProperty<bool> IsInstalled { get; } = new(value: false);
    public BindableReactiveProperty<bool> IsDownloading { get; } = new();
    public BindableReactiveProperty<bool> IsInstalling { get; } = new();
    public BindableReactiveProperty<bool> IsUpdateAvailable { get; } = new();
    public BindableReactiveProperty<bool> HasInstalledAllOptionalItems { get; } = new(value: false);
    public BindableReactiveProperty<Optional<RevisionNumber>> NewestRevisionNumber { get; } = new();

    public ReactiveCommand<NavigationInformation> CommandViewCollection { get; } = new();
    public ReactiveCommand<Unit> CommandDownloadOptionalItems { get; } = new ReactiveCommand();
    public ReactiveCommand<Unit> CommandDownloadRequiredItems { get; } = new ReactiveCommand();
    public ReactiveCommand<Unit> CommandInstallOptionalItems { get; } = new ReactiveCommand();
    public ReactiveCommand<Unit> CommandInstallRequiredItems { get; } = new ReactiveCommand();
    public ReactiveCommand<Unit> CommandUpdateCollection { get; } = new ReactiveCommand();

    public ReactiveCommand<Unit> CommandViewOnNexusMods { get; } = new ReactiveCommand();
    public ReactiveCommand<Unit> CommandOpenJsonFile { get; } = new ReactiveCommand();
    public ReactiveCommand<Unit> CommandDeleteAllDownloads { get; } = new ReactiveCommand();
    public ReactiveCommand<Unit> CommandDeleteCollectionRevision { get; } = new ReactiveCommand();
}
