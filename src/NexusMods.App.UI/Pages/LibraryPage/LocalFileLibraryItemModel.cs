using Avalonia.Media.Imaging;
using DynamicData;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

[Obsolete("Use CompositeItemModel instead")]
public class LocalFileLibraryItemModel : TreeDataGridItemModel<ILibraryItemModel, EntityId>,
    ILibraryItemWithThumbnailAndName,
    ILibraryItemWithSize,
    ILibraryItemWithDates,
    ILibraryItemWithInstallAction,
    IHasLinkedLoadoutItems,
    IIsChildLibraryItemModel
{
    public LocalFileLibraryItemModel(LocalFile.ReadOnly localFile, IServiceProvider serviceProvider)
    {
        LibraryItemId = localFile.Id;

        FormattedSize = ItemSize.ToFormattedProperty();
        FormattedDownloadedDate = DownloadedDate.ToFormattedProperty();
        FormattedInstalledDate = InstalledDate.ToFormattedProperty();
        InstallItemCommand = ILibraryItemWithInstallAction.CreateCommand(this);

        // Note: Because this is a local file, this always hits the fallback thumbnail in practice.
        var modPageThumbnailPipeline = ImagePipelines.GetModPageThumbnailPipeline(serviceProvider);
        var imageDisposable = ImagePipelines.CreateObservable(localFile.Id, modPageThumbnailPipeline)
            .ObserveOnUIThreadDispatcher()
            .Subscribe(this, static (bitmap, self) => self.Thumbnail.Value = bitmap);
        
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
            imageDisposable,
            linkedLoadoutItemsDisposable,
            modelActivationDisposable,
            Name,
            ItemSize,
            FormattedSize,
            DownloadedDate,
            FormattedDownloadedDate,
            InstalledDate,
            FormattedInstalledDate,
            InstallItemCommand,
            IsInstalled,
            InstallButtonText,
            Thumbnail
        );
    }

    public LibraryItemId LibraryItemId { get; }

    public Observable<DateTimeOffset>? Ticker { get; set; }

    public required IObservable<IChangeSet<LibraryLinkedLoadoutItem.ReadOnly, EntityId>> LinkedLoadoutItemsObservable { get; init; }
    public ObservableDictionary<EntityId, LibraryLinkedLoadoutItem.ReadOnly> LinkedLoadoutItems { get; private set; } = [];

    public BindableReactiveProperty<Bitmap> Thumbnail { get; set; } = new();
    public BindableReactiveProperty<string> Name { get; } = new(value: "-");
    public BindableReactiveProperty<bool> ShowThumbnail { get; } = new(value: true);

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

    public override string ToString() => $"Local File Child: {Name.Value}";
}
