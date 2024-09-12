using DynamicData;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public class FakeParentLoadoutItemModel : LoadoutItemModel
{
    public required IObservable<DateTime> InstalledAtObservable { get; init; }

    public required IObservable<IChangeSet<LoadoutItemId, EntityId>> LoadoutItemIdsObservable { get; init; }
    public ObservableHashSet<LoadoutItemId> LoadoutItemIds { get; private set; } = [];

    public override IReadOnlyCollection<LoadoutItemId> GetLoadoutItemIds() => LoadoutItemIds;

    private readonly IDisposable _modelActivationDisposable;
    private readonly SerialDisposable _loadoutItemIdsDisposable = new();

    public FakeParentLoadoutItemModel() : base(default(LoadoutItemId))
    {
        _modelActivationDisposable = WhenModelActivated(this, static (model, disposables) =>
        {
            model.InstalledAtObservable.OnUI().Subscribe(date => model.InstalledAt.Value = date).AddTo(disposables);

            if (model._loadoutItemIdsDisposable.Disposable is null)
            {
                model._loadoutItemIdsDisposable.Disposable = model.LoadoutItemIdsObservable.OnUI().SubscribeWithErrorLogging(changeSet => model.LoadoutItemIds.ApplyChanges(changeSet));
            }
        });
    }

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                Disposable.Dispose(_modelActivationDisposable, _loadoutItemIdsDisposable);
            }

            LoadoutItemIds = null!;

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
