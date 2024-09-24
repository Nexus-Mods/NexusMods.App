using DynamicData;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

public class NexusModsModPageLibraryItemModel : FakeParentLibraryItemModel
{
    private readonly IDisposable _modelActivationDisposable;
    public NexusModsModPageLibraryItemModel(IObservable<IChangeSet<LibraryItem.ReadOnly, EntityId>> libraryItemsObservable) 
        : base(default(LibraryItemId), libraryItemsObservable)
    {
        _modelActivationDisposable = WhenModelActivated(this, static (model, disposables) =>
        {
            model.LibraryItems
                .ObserveCountChanged(notifyCurrentCount: true)
                .Subscribe(model, static (count, model) =>
                {
                    if (count == 0)
                    {
                        model.CreatedAtDate.Value = DateTime.UnixEpoch;
                        model.ItemSize.Value = Size.Zero.ToString();
                        model.Version.Value = "-";
                    }
                    else
                    {
                        model.CreatedAtDate.Value = model.LibraryItems.Max(x => x.GetCreatedAt());
                        model.ItemSize.Value = model.LibraryItems.Sum(x => x.ToLibraryFile().Size).ToString();

                        // TODO: "mod page"-version, whatever that means
                        model.Version.Value = "-";
                    }

                    model.FormattedCreatedAtDate.Value = FormatDate(DateTime.Now, model.CreatedAtDate.Value);
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
