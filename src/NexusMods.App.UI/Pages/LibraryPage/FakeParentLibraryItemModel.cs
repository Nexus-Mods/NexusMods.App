using DynamicData;
using NexusMods.Abstractions.Library.Models;
using NexusMods.App.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using ObservableCollections;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LibraryPage;

public class FakeParentLibraryItemModel : LibraryItemModel
{
    public required IObservable<int> NumInstalledObservable { get; init; }
    public IObservable<IChangeSet<LibraryItem.ReadOnly, EntityId>> LibraryItemsObservable { get; }
    protected ObservableHashSet<LibraryItem.ReadOnly> LibraryItems { get; set; } = [];

    public override IReadOnlyCollection<LibraryItemId> GetLoadoutItemIds() => LibraryItems.Select(static item => item.LibraryItemId).ToArray();

    private readonly IDisposable _modelActivationDisposable;
    private readonly IDisposable _libraryItemsDisposable;

    public FakeParentLibraryItemModel(LibraryItemId libraryItemId, IObservable<IChangeSet<LibraryItem.ReadOnly, EntityId>> libraryItemsObservable) : base(libraryItemId)
    {
        LibraryItemsObservable = libraryItemsObservable;
        
        // NOTE(Al12rs): This needs to be set up even if model is never activated,
        // as it is possible for items to get selected and interacted with without their model being activated
        // (e.g. by quick scrolling to bottom with scrollbar and shift-selecting all items)
        _libraryItemsDisposable =  LibraryItemsObservable.OnUI().SubscribeWithErrorLogging(changeSet => LibraryItems.ApplyChanges(changeSet));
        
        _modelActivationDisposable = WhenModelActivated(this, static (model, disposables) =>
        {
            model.NumInstalledObservable
                .ToObservable()
                .CombineLatest(
                    source2: model.LibraryItems.ObserveCountChanged(),
                    source3: model.WhenAnyValue(static model => model.IsExpanded).ToObservable(),
                    source4: model.IsInstalledInLoadout,
                    static (a,b,c , _) => (a,b,c)
                )
                .ObserveOnUIThreadDispatcher()
                .Subscribe(model, static (tuple, model) =>
                {
                    var (numInstalled, numCount, isExpanded) = tuple;

                    if (numInstalled > 0)
                    {
                        if (numInstalled == numCount)
                        {
                            model.InstallText.Value = "Installed";
                        } else {
                            model.InstallText.Value = $"Installed {numInstalled}/{numCount}";
                        }
                    } else {
                        if (!isExpanded && numCount == 1)
                        {
                            model.InstallText.Value = "Install";
                        } else {
                            model.InstallText.Value = $"Install ({numCount})";
                        }
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
                Disposable.Dispose(_modelActivationDisposable, _libraryItemsDisposable);
            }

            LibraryItems = null!;
            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
