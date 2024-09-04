using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Extensions.BCL;
using NexusMods.Paths;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

public class NexusModsModPageLibraryItemModel : FakeParentLibraryItemModel
{
    private readonly IDisposable _modelActivationDisposable;
    public NexusModsModPageLibraryItemModel() : base(default(LibraryItemId))
    {
        _modelActivationDisposable = WhenModelActivated(this, static (model, disposables) =>
        {
            model.LibraryItems
                .ObserveCountChanged(notifyCurrentCount: true)
                .Subscribe(model, static (_, model) =>
                {
                    // TODO: different selection, need to check with design
                    if (model.LibraryItems.TryGetFirst(static x => x.ToLibraryFile().ToDownloadedFile().ToNexusModsLibraryFile().IsValid(), out var libraryItem))
                    {
                        model.ItemSize.Value = libraryItem.ToLibraryFile().Size.ToString();
                        model.Version.Value = libraryItem.ToLibraryFile().ToDownloadedFile().ToNexusModsLibraryFile().FileMetadata.Version;
                    }
                    else
                    {
                        model.ItemSize.Value = Size.Zero.ToString();
                        model.Version.Value = "-";
                    }
                })
                .AddTo(disposables);
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

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }

    public override string ToString() => $"{base.ToString()} (Mod Page)";
}
