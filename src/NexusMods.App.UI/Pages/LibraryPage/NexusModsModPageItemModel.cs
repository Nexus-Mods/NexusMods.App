using DynamicData;
using DynamicData.Binding;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Extensions.BCL;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

public class NexusModsModPageItemModel : LibraryItemModel
{
    public required IObservable<IChangeSet<NexusModsLibraryFile.ReadOnly>> LibraryFilesObservable { get; init; }
    private ObservableCollectionExtended<NexusModsLibraryFile.ReadOnly> LibraryFiles { get; set; } = [];

    private readonly IDisposable _modelActivationDisposable;
    public NexusModsModPageItemModel()
    {
        _modelActivationDisposable = WhenModelActivated(this, static (model, disposables) =>
        {
            model.LibraryFiles
                .ObserveCollectionChanges()
                .SubscribeWithErrorLogging(_ =>
                {
                    // TODO: different selection, need to check with design
                    if (model.LibraryFiles.TryGetFirst(out var primaryFile))
                    {
                        model.Size = primaryFile.AsDownloadedFile().AsLibraryFile().Size;
                        model.Version = primaryFile.FileMetadata.Version;
                        model.LibraryItemId = primaryFile.AsDownloadedFile().AsLibraryFile().AsLibraryItem().LibraryItemId;
                    }
                    else
                    {
                        model.Size = Size.Zero;
                        model.Version = "-";
                        model.LibraryItemId = DynamicData.Kernel.Optional<LibraryItemId>.None;
                    }
                })
                .AddTo(disposables);

            model.LibraryFilesObservable.OnUI().Bind(model.LibraryFiles).SubscribeWithErrorLogging().AddTo(disposables);
            Disposable.Create(model.LibraryFiles, static libraryFiles => libraryFiles.Clear()).AddTo(disposables);
        });
    }

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _modelActivationDisposable.Dispose();
            }

            LibraryFiles = null!;
            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
