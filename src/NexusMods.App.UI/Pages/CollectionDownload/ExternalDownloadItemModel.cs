using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Pages.CollectionDownload;

public class ExternalDownloadItemModel : TreeDataGridItemModel<ILibraryItemModel, EntityId>,
    ILibraryItemWithName,
    ILibraryItemWithSize,
    ILibraryItemWithDownloadAction
{
    public ExternalDownloadItemModel(CollectionDownloadExternal.ReadOnly externalDownload)
    {
        DownloadableItem = new DownloadableItem(new ExternalItem(externalDownload.Uri, externalDownload.Size, externalDownload.Md5));
        FormattedSize = ItemSize.ToFormattedProperty();
        DownloadItemCommand = ILibraryItemWithDownloadAction.CreateCommand(this);

        // ReSharper disable once NotDisposedResource
        var modelActivationDisposable = this.WhenActivated(static (self, disposables) =>
        {
            self.IsInLibraryObservable.ObserveOnUIThreadDispatcher()
                .Subscribe(self, static (inLibrary, self) =>
                {
                    self.DownloadState.Value = inLibrary ? JobStatus.Completed : JobStatus.None;
                    self.DownloadButtonText.Value = ILibraryItemWithDownloadAction.GetButtonText(status: self.DownloadState.Value);
                }).AddTo(disposables);
        });

        _modelDisposable = Disposable.Combine(
            modelActivationDisposable,
            Name,
            ItemSize,
            FormattedSize,
            DownloadItemCommand,
            DownloadState,
            DownloadButtonText
        );
    }

    public required Observable<bool> IsInLibraryObservable { get; init; }
    // public required Observable<IJob> DownloadJobObservable { get; init; }

    public BindableReactiveProperty<string> Name { get; } = new(value: "-");

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

    public override string ToString() => $"External Download: {Name.Value}";
}
