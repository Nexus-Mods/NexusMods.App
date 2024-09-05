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
                .Subscribe(model, static (count, model) =>
                {
                    if (count == 0)
                    {
                        model.ItemSize.Value = Size.Zero.ToString();
                        model.Version.Value = "-";
                    }
                    else
                    {
                        model.ItemSize.Value = model.LibraryItems.Sum(x => x.ToLibraryFile().Size).ToString();

                        // TODO: "mod page"-version, whatever that means
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
