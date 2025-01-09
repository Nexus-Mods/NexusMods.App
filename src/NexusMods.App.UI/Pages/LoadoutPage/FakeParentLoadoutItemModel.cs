using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public class FakeParentLoadoutItemModel : LoadoutItemModel
{
    public required IObservable<DateTimeOffset> InstalledAtObservable { get; init; }

    public IObservable<IChangeSet<LoadoutItemId, EntityId>> LoadoutItemIdsObservable { get; }
    public ObservableHashSet<LoadoutItemId> LoadoutItemIds { get; private set; } = [];

    public override IReadOnlyCollection<LoadoutItemId> GetLoadoutItemIds() => LoadoutItemIds;

    private readonly IDisposable _modelActivationDisposable;
    private readonly IDisposable _loadoutItemIdsDisposable;
    private readonly IDisposable _childrenObservableDisposable;

    public FakeParentLoadoutItemModel(IObservable<IChangeSet<LoadoutItemId, EntityId>> loadoutItemIdsObservable, IServiceProvider provider, IConnection connection) : base(default(LoadoutItemId), provider, provider.GetRequiredService<IConnection>(), true)
    {
        LoadoutItemIdsObservable = loadoutItemIdsObservable;
        _loadoutItemIdsDisposable = LoadoutItemIdsObservable.OnUI().SubscribeWithErrorLogging(changeSet => LoadoutItemIds.ApplyChanges(changeSet));
        
        // Inherit the icon from the first child
        _childrenObservableDisposable = ChildrenObservable.FirstAsync().Subscribe(set =>
        {
            foreach (var item in set)
            {
                if (item.Reason != ChangeReason.Add) continue;
                        
                // Note(sewer):
                // The child may not be activated, so we can't just copy the thumbnail, from it as it may
                // not have been loaded yet. We need to manually load it.
                var current = item.Current;
                var modPageThumbnailPipeline = ImagePipelines.GetModPageThumbnailPipeline(provider);
                var libraryLinkedItem = LibraryLinkedLoadoutItem.Load(connection.Db, current.GetLoadoutItemIds().First());
                if (libraryLinkedItem.IsValid() && libraryLinkedItem.LibraryItem.TryGetAsNexusModsLibraryItem(out var nexusLibraryItem))
                {
                    ImagePipelines.CreateObservable(nexusLibraryItem.ModPageMetadataId, modPageThumbnailPipeline)
                        .Take(1)
                        .ObserveOnUIThreadDispatcher()
                        .Subscribe(this, (bitmap, _) => Thumbnail.Value = bitmap);
                }
                        
                return;
            }
        });
        
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
                Disposable.Dispose(_modelActivationDisposable, _loadoutItemIdsDisposable, _childrenObservableDisposable);
            }

            LoadoutItemIds = null!;

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
