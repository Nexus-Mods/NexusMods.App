using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public class CollectionLoadoutDesignViewModel : APageViewModel<ICollectionLoadoutViewModel>, ICollectionLoadoutViewModel
{
    public CollectionLoadoutDesignViewModel() : base(new DesignWindowManager()) { }

    public LoadoutTreeDataGridAdapter Adapter { get; } = null!;
    public bool IsCollectionEnabled => true;
    public string Name => "Vanilla+ [Quality of Life]";
    public RevisionNumber RevisionNumber { get; } = RevisionNumber.From(6);
    public string AuthorName => "Lowtonotolerance";
    public Bitmap? AuthorAvatar => new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/avatar.webp")));
    public Bitmap TileImage { get; } = new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/collection_tile_image.png")));
    public Bitmap BackgroundImage { get; } = new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/header-background.webp")));
    public ReactiveCommand<Unit> CommandToggle { get; } = new ReactiveCommand();
    public bool IsLocalCollection { get; } = false;
    public bool IsReadOnly { get; } = true;
}
