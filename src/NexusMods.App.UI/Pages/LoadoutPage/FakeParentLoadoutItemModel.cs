using DynamicData;
using DynamicData.Binding;
using NexusMods.Abstractions.Loadouts;
using R3;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public class FakeParentLoadoutItemModel : LoadoutItemModel
{
    public required IObservable<DateTime> InstalledAtObservable { get; init; }

    public required IObservable<IChangeSet<LoadoutItemId>> LoadoutItemIdsObservable { get; init; }
    public ObservableCollectionExtended<LoadoutItemId> LoadoutItemIds { get; private set; } = [];

    public override IReadOnlyCollection<LoadoutItemId> GetLoadoutItemIds() => LoadoutItemIds;

    private readonly IDisposable _modelActivationDisposable;
    public FakeParentLoadoutItemModel() : base(default(LoadoutItemId))
    {
        _modelActivationDisposable = WhenModelActivated(this, static (model, disposables) =>
        {
            model.InstalledAtObservable.OnUI().Subscribe(date => model.InstalledAt = date).AddTo(disposables);

            model.LoadoutItemIdsObservable.OnUI().Bind(model.LoadoutItemIds).SubscribeWithErrorLogging().AddTo(disposables);
            Disposable.Create(model.LoadoutItemIds, static collection => collection.Clear()).AddTo(disposables);
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
}
