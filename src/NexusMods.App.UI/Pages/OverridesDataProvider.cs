using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.App.UI.Pages;

[UsedImplicitly]
public class OverridesDataProvider : ILoadoutDataProvider
{
    private readonly IConnection _connection;

    public OverridesDataProvider(IConnection connection)
    {
        _connection = connection;
    }

    public IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObserveLoadoutItems(LoadoutFilter loadoutFilter)
    {
        // Only show in default My Mods collection group and All
        if (loadoutFilter.CollectionGroupId.HasValue)
        {
            var collectionGroup = CollectionGroup.Load(_connection.Db, loadoutFilter.CollectionGroupId.Value);

            // TODO: Make this check more robust
            if (!collectionGroup.AsLoadoutItemGroup().AsLoadoutItem().Name.Equals("My Mods"))
            {
                return Observable.Return(new ChangeSet<CompositeItemModel<EntityId>, EntityId>());
            }
        }
        
        var overridesObservable = _connection.ObserveDatoms(LoadoutOverridesGroup.OverridesFor, loadoutFilter.LoadoutId)
            .AsEntityIds()
            .Transform(datom => LoadoutOverridesGroup.Load(_connection.Db, datom.E))
            .RefCount();
        var loadoutItemObservable = overridesObservable.Transform(item => item.AsLoadoutItemGroup().AsLoadoutItem());

        return overridesObservable.Transform(overridesGroup =>
            {
                var hasChildrenObservable = overridesObservable.IsNotEmpty();
                var childrenObservable = overridesObservable.Transform(item => LoadoutDataProviderHelper.ToChildItemModel(_connection, item.AsLoadoutItemGroup().AsLoadoutItem()));

                var parentItemModel = new CompositeItemModel<EntityId>(overridesGroup.Id)
                {
                    HasChildrenObservable = hasChildrenObservable,
                    ChildrenObservable = childrenObservable,
                };
                
                parentItemModel.Add(SharedColumns.Name.StringComponentKey, new StringComponent(value: overridesGroup.AsLoadoutItemGroup().AsLoadoutItem().Name));
                parentItemModel.Add(SharedColumns.Name.ImageComponentKey, new ImageComponent(value: ImagePipelines.ModPageThumbnailFallback));
                
                LoadoutDataProviderHelper.AddDateComponent(parentItemModel, overridesGroup.GetCreatedAt(), loadoutItemObservable);
                LoadoutDataProviderHelper.AddIsEnabled(_connection, parentItemModel, loadoutItemObservable);
                
                return parentItemModel;
            }
        );
    }
}
