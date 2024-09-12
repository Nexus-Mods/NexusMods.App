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
    public required IObservable<IChangeSet<LibraryItem.ReadOnly, EntityId>> LibraryItemsObservable { get; init; }
    protected ObservableList<LibraryItem.ReadOnly> LibraryItems { get; set; } = [];

    public override IReadOnlyCollection<LibraryItemId> GetLoadoutItemIds() => LibraryItems.Select(static item => item.LibraryItemId).ToArray();

    private readonly IDisposable _modelActivationDisposable;
    private readonly IDisposable _activationSelectionDisposable;

    public FakeParentLibraryItemModel(LibraryItemId libraryItemId) : base(libraryItemId)
    {
        _activationSelectionDisposable = Activation.CombineLatest(IsSelected, (a, b) => (a, b)).Subscribe(this, static (tuple, self) =>
        {
            var (isActivating, isSelected) = tuple;
            if (!isActivating && !isSelected)
            {
                self.LibraryItems.Clear();
            }
        });

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

            model.LibraryItemsObservable.OnUI().SubscribeWithErrorLogging(changeSet => model.LibraryItems.ApplyChanges(changeSet)).AddTo(disposables);
        });
    }

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                Disposable.Dispose(_modelActivationDisposable, _activationSelectionDisposable);
            }

            LibraryItems = null!;
            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
