using Avalonia.Media.Imaging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

/// <summary>
///     This is used for downloadable collection items.
/// </summary>
public class NexusModsFileMetadataLibraryItemModel : TreeDataGridItemModel<ILibraryItemModel, EntityId>,
    ILibraryItemWithThumbnail,
    ILibraryItemWithName,
    ILibraryItemWithVersion,
    ILibraryItemWithSize,
    ILibraryItemWithDownloadAction
{
    public NexusModsFileMetadataLibraryItemModel(CollectionDownloadNexusMods.ReadOnly download, IServiceProvider serviceProvider)
    {
        DownloadableItem = new DownloadableItem(download);
        FormattedSize = ItemSize.ToFormattedProperty();
        DownloadItemCommand = ILibraryItemWithDownloadAction.CreateCommand(this);
        
        var modPageThumbnailPipeline = ImagePipelines.GetModPageThumbnailPipeline(serviceProvider);
        var thumbnailDisposable = ImagePipelines.CreateObservable(download.FileMetadata.ModPage.Id, modPageThumbnailPipeline)
            .ObserveOnUIThreadDispatcher()
            .Subscribe(this, static (bitmap, self) => self.Thumbnail.Value = bitmap);

        // ReSharper disable once NotDisposedResource
        var modelActivationDisposable = this.WhenActivated(static (self, disposables) =>
        {
            self.IsInLibraryObservable.CombineLatest(
                source2: self.DownloadJobObservable.SelectMany(job => job.ObservableStatus.ToObservable()).Prepend(JobStatus.None),
                resultSelector: static (a, b) => (a, b))
                .ObserveOnUIThreadDispatcher()
                .Subscribe(self, static (tuple, self) =>
                {
                    var (inLibrary, status) = tuple;
                    self.DownloadState.Value = inLibrary ? JobStatus.Completed : status;
                    self.DownloadButtonText.Value = ILibraryItemWithDownloadAction.GetButtonText(status: self.DownloadState.Value);
                }).AddTo(disposables);
        });

        _modelDisposable = Disposable.Combine(
            modelActivationDisposable,
            Name,
            Version,
            ItemSize,
            FormattedSize,
            DownloadItemCommand,
            DownloadState,
            DownloadButtonText,
            thumbnailDisposable
        );
    }

    public required Observable<bool> IsInLibraryObservable { get; init; }
    public required Observable<IJob> DownloadJobObservable { get; init; }

    public BindableReactiveProperty<Bitmap> Thumbnail { get; } = new();
    public BindableReactiveProperty<string> Name { get; } = new(value: "-");
    public BindableReactiveProperty<string> Version { get; } = new(value: "-");

    public ReactiveProperty<Size> ItemSize { get; } = new();
    public BindableReactiveProperty<string> FormattedSize { get; }

    public DownloadableItem DownloadableItem { get; }

    public ReactiveCommand<Unit, DownloadableItem> DownloadItemCommand { get; }

    public BindableReactiveProperty<JobStatus> DownloadState { get; } = new();

    public BindableReactiveProperty<string> DownloadButtonText { get; } = new(value: ILibraryItemWithDownloadAction.GetButtonText(status: JobStatus.None));

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

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }

    public override string ToString() => $"Nexus Mods File Metadata: {Name.Value}";
}

