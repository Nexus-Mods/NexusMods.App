using Avalonia.Media.Imaging;
using DynamicData;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

/// <summary>
///     This is used for individual files (archives) linked to a download page.
/// </summary>
[Obsolete("Use CompositeItemModel instead")]
public class NexusModsFileLibraryItemModel : TreeDataGridItemModel<ILibraryItemModel, EntityId>,
    ILibraryItemWithThumbnailAndName,
    ILibraryItemWithVersion,
    ILibraryItemWithSize,
    ILibraryItemWithDates,
    ILibraryItemWithInstallAction,
    IHasLinkedLoadoutItems,
    IIsChildLibraryItemModel
{
    public NexusModsFileLibraryItemModel(NexusModsLibraryItem.ReadOnly nexusModsLibraryItem, IServiceProvider serviceProvider, bool showThumbnail = true)
    {
        LibraryItemId = nexusModsLibraryItem.Id;

        FormattedSize = ItemSize.ToFormattedProperty();
        FormattedDownloadedDate = DownloadedDate.ToFormattedProperty();
        FormattedInstalledDate = InstalledDate.ToFormattedProperty();
        InstallItemCommand = ILibraryItemWithInstallAction.CreateCommand(this);

        var imageDisposable = Disposable.Empty;
        ShowThumbnail.Value = showThumbnail;
        if (showThumbnail)
        {
            var modPageThumbnailPipeline = ImagePipelines.GetModPageThumbnailPipeline(serviceProvider);
            imageDisposable = ImagePipelines.CreateObservable(nexusModsLibraryItem.ModPageMetadataId, modPageThumbnailPipeline)
                .ObserveOnUIThreadDispatcher()
                .Subscribe(this, static (bitmap, self) => self.Thumbnail.Value = bitmap);
        }
        
        // ReSharper disable once NotDisposedResource
        var datesDisposable = ILibraryItemWithDates.SetupDates(this);

        var linkedLoadoutItemsDisposable = new SerialDisposable();

        // ReSharper disable once NotDisposedResource
        var modelActivationDisposable = this.WhenActivated(linkedLoadoutItemsDisposable, static (self, linkedLoadoutItemsDisposable, disposables) =>
        {
            // ReSharper disable once NotDisposedResource
            IHasLinkedLoadoutItems.SetupLinkedLoadoutItems(self, linkedLoadoutItemsDisposable).AddTo(disposables);
        });

        _modelDisposable = Disposable.Combine(
            datesDisposable,
            linkedLoadoutItemsDisposable,
            modelActivationDisposable,
            Name,
            Version,
            ItemSize,
            FormattedSize,
            DownloadedDate,
            FormattedDownloadedDate,
            InstalledDate,
            FormattedInstalledDate,
            InstallItemCommand,
            IsInstalled,
            InstallButtonText,
            imageDisposable
        );
    }

    public LibraryItemId LibraryItemId { get; }

    public Observable<DateTimeOffset>? Ticker { get; set; }

    public required IObservable<IChangeSet<LibraryLinkedLoadoutItem.ReadOnly, EntityId>> LinkedLoadoutItemsObservable { get; init; }
    public ObservableDictionary<EntityId, LibraryLinkedLoadoutItem.ReadOnly> LinkedLoadoutItems { get; private set; } = [];

    public BindableReactiveProperty<Bitmap> Thumbnail { get; } = new();
    public BindableReactiveProperty<bool> ShowThumbnail { get; } = new(value: true);
    public BindableReactiveProperty<string> Name { get; } = new(value: "-");
    public BindableReactiveProperty<string> Version { get; } = new(value: "-");

    public ReactiveProperty<Size> ItemSize { get; } = new();
    public BindableReactiveProperty<string> FormattedSize { get; }

    public ReactiveProperty<DateTimeOffset> DownloadedDate { get; } = new();
    public BindableReactiveProperty<string> FormattedDownloadedDate { get; }

    public ReactiveProperty<DateTimeOffset> InstalledDate { get; } = new();
    public BindableReactiveProperty<string> FormattedInstalledDate { get; }

    public ReactiveCommand<Unit, ILibraryItemModel> InstallItemCommand { get; }
    public BindableReactiveProperty<bool> IsInstalled { get; } = new();
    public BindableReactiveProperty<string> InstallButtonText { get; } = new(value: ILibraryItemWithInstallAction.GetButtonText(isInstalled: false));

    private bool _isDisposed;
    private readonly IDisposable _modelDisposable;

    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _modelDisposable.Dispose();
            }

            LinkedLoadoutItems = null!;
            _isDisposed = true;
        }

        base.Dispose(disposing);
    }

    public override string ToString() => $"Nexus Mods File: {Name.Value}";
}
