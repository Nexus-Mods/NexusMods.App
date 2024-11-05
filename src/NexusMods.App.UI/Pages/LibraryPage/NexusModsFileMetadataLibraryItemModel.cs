using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

public class NexusModsFileMetadataLibraryItemModel : TreeDataGridItemModel<ILibraryItemModel, EntityId>,
    ILibraryItemWithName,
    ILibraryItemWithVersion,
    ILibraryItemWithSize,
    ILibraryItemWithDownloadAction
{
    public NexusModsFileMetadataLibraryItemModel(NexusModsFileMetadata.ReadOnly fileMetadata)
    {
        FormattedSize = ItemSize.ToFormattedProperty();
        DownloadItemCommand = ILibraryItemWithDownloadAction.CreateCommand(this);

        _modelDisposable = Disposable.Combine(
            Name,
            Version,
            ItemSize,
            FormattedSize,
            DownloadItemCommand,
            DownloadState,
            DownloadButtonText
        );
    }

    public BindableReactiveProperty<string> Name { get; } = new(value: "-");
    public BindableReactiveProperty<string> Version { get; } = new(value: "-");

    public ReactiveProperty<Size> ItemSize { get; } = new();
    public BindableReactiveProperty<string> FormattedSize { get; }

    public ReactiveCommand<Unit, Unit> DownloadItemCommand { get; }

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

