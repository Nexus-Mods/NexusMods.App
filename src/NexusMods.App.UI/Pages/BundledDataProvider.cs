using System.Reactive.Linq;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.App.UI.Pages;

[UsedImplicitly]
public class BundledDataProvider : ILoadoutDataProvider
{
    private readonly IConnection _connection;

    public BundledDataProvider(IServiceProvider serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
    }

    private IObservable<IChangeSet<Datom, EntityId>> FilterLoadoutItems(LoadoutFilter loadoutFilter)
    {
        return _connection.ObserveDatoms(NexusCollectionBundledLoadoutGroup.PrimaryAttribute)
            .AsEntityIds()
            .FilterInStaticLoadout(_connection, loadoutFilter);
    }

    public IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObserveLoadoutItems(LoadoutFilter loadoutFilter)
    {
        return FilterLoadoutItems(loadoutFilter)
            .TransformImmutable(datom => NexusCollectionBundledLoadoutGroup.Load(_connection.Db, datom.E))
            .Transform(ToLoadoutItemModel);
    }

    public IObservable<int> CountLoadoutItems(LoadoutFilter loadoutFilter)
    {
        return FilterLoadoutItems(loadoutFilter).QueryWhenChanged(static query => query.Count).Prepend(0);
    }

    private CompositeItemModel<EntityId> ToLoadoutItemModel(NexusCollectionBundledLoadoutGroup.ReadOnly item)
    {
        var loadoutItem = item.AsNexusCollectionItemLoadoutGroup().AsLoadoutItemGroup().AsLoadoutItem();

        var hasChildrenObservable = Observable.Return(true);
        var childrenObservable = Observable.Return(
            new ChangeSet<CompositeItemModel<EntityId>, EntityId>(
                [
                    new Change<CompositeItemModel<EntityId>, EntityId>(ChangeReason.Add, item.Id, LoadoutDataProviderHelper.ToChildItemModel(_connection, loadoutItem)),
                ]
            ));

        var parentItemModel = new CompositeItemModel<EntityId>(item.Id)
        {
            HasChildrenObservable = hasChildrenObservable,
            ChildrenObservable = childrenObservable,
        };

        parentItemModel.Add(SharedColumns.Name.StringComponentKey, new StringComponent(value: item.AsNexusCollectionItemLoadoutGroup().AsLoadoutItemGroup().AsLoadoutItem().Name));
        parentItemModel.Add(SharedColumns.Name.ImageComponentKey, new ImageComponent(value: ImagePipelines.ModPageThumbnailFallback));
        parentItemModel.Add(SharedColumns.InstalledDate.ComponentKey, new DateComponent(value: item.GetCreatedAt()));

        LoadoutDataProviderHelper.AddCollection(_connection, parentItemModel, loadoutItem);
        LoadoutDataProviderHelper.AddIsEnabled(_connection, parentItemModel, loadoutItem);

        return parentItemModel;
    }
}
