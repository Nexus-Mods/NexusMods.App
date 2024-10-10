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

    public IObservable<IChangeSet<LoadoutItemId, EntityId>> LoadoutItemIdsObservable { get; }
    public ObservableHashSet<LoadoutItemId> LoadoutItemIds { get; private set; } = [];

    public override IReadOnlyCollection<LoadoutItemId> GetLoadoutItemIds() => LoadoutItemIds;

    private readonly IDisposable _modelActivationDisposable;
    private readonly IDisposable _loadoutItemIdsDisposable;

    public FakeParentLoadoutItemModel(IObservable<IChangeSet<LoadoutItemId, EntityId>> loadoutItemIdsObservable) : base(default(LoadoutItemId))
    {
        LoadoutItemIdsObservable = loadoutItemIdsObservable;
        _loadoutItemIdsDisposable = LoadoutItemIdsObservable.OnUI().SubscribeWithErrorLogging(changeSet => LoadoutItemIds.ApplyChanges(changeSet));
        
        _modelActivationDisposable = WhenModelActivated(this, static (model, disposables) =>
        {
            model.InstalledAtObservable.OnUI().Subscribe(date => model.InstalledAt.Value = date).AddTo(disposables);
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
