using System.ComponentModel;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.MnemonicDB.Abstractions;
using OneOf;
using R3;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public readonly record struct ToggleEnableStateMessage(LoadoutItemId[] Ids);

public readonly record struct OpenCollectionMessage(LoadoutItemId[] Ids, NavigationInformation NavigationInformation);

public class LoadoutTreeDataGridAdapter :
    TreeDataGridAdapter<CompositeItemModel<EntityId>, EntityId>,
    ITreeDataGirdMessageAdapter<OneOf<ToggleEnableStateMessage, OpenCollectionMessage>>
{
    public Subject<OneOf<ToggleEnableStateMessage, OpenCollectionMessage>> MessageSubject { get; } = new();

    private readonly ILoadoutDataProvider[] _loadoutDataProviders;
    private readonly LoadoutFilter _loadoutFilter;

    public LoadoutTreeDataGridAdapter(IServiceProvider serviceProvider, LoadoutFilter loadoutFilter)
    {
        _loadoutDataProviders = serviceProvider.GetServices<ILoadoutDataProvider>().ToArray();
        _loadoutFilter = loadoutFilter;
    }

    protected override IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> GetRootsObservable(bool viewHierarchical)
    {
        return _loadoutDataProviders.Select(x => x.ObserveLoadoutItems(_loadoutFilter)).MergeChangeSets();
    }

    protected override void BeforeModelActivationHook(CompositeItemModel<EntityId> model)
    {
        base.BeforeModelActivationHook(model);

        model.SubscribeToComponentAndTrack<LoadoutComponents.EnabledStateToggle, LoadoutTreeDataGridAdapter>(
            key: LoadoutColumns.EnabledState.EnabledStateToggleComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandToggle.Subscribe((self, itemModel, component), static (_, tuple) =>
                {
                    var (self, itemModel, _) = tuple;
                    var ids = GetLoadoutItemIds(itemModel).ToArray();

                    self.MessageSubject.OnNext(new ToggleEnableStateMessage(ids));
                }
            )
        );

        model.SubscribeToComponentAndTrack<LoadoutComponents.ParentCollectionDisabled, LoadoutTreeDataGridAdapter>(
            key: LoadoutColumns.EnabledState.ParentCollectionDisabledComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.ButtonCommand.Subscribe((self, itemModel, component), static (navInfo, tuple) =>
                {
                    var (self, itemModel, _) = tuple;
                    var ids = GetLoadoutItemIds(itemModel).ToArray();

                    self.MessageSubject.OnNext(new OpenCollectionMessage(ids, navInfo));
                }
            )
        );

        model.SubscribeToComponentAndTrack<LoadoutComponents.LockedEnabledState, LoadoutTreeDataGridAdapter>(
            key: LoadoutColumns.EnabledState.LockedEnabledStateComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.ButtonCommand.Subscribe((self, itemModel, component), static (navInfo, tuple) =>
                {
                    var (self, itemModel, _) = tuple;
                    var ids = GetLoadoutItemIds(itemModel).ToArray();

                    self.MessageSubject.OnNext(new OpenCollectionMessage(ids, navInfo));
                }
            )
        );

        model.SubscribeToComponentAndTrack<LoadoutComponents.MixLockedAndParentDisabled, LoadoutTreeDataGridAdapter>(
            key: LoadoutColumns.EnabledState.MixLockedAndParentDisabledComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.ButtonCommand.Subscribe((self, itemModel, component), static (navInfo, tuple) =>
                {
                    var (self, itemModel, _) = tuple;
                    var ids = GetLoadoutItemIds(itemModel).ToArray();

                    self.MessageSubject.OnNext(new OpenCollectionMessage(ids, navInfo));
                }
            )
        );
    }

    private static IEnumerable<LoadoutItemId> GetLoadoutItemIds(CompositeItemModel<EntityId> itemModel)
    {
        return itemModel.Get<LoadoutComponents.LoadoutItemIds>(LoadoutColumns.EnabledState.LoadoutItemIdsComponentKey).ItemIds;
    }

    protected override IColumn<CompositeItemModel<EntityId>>[] CreateColumns(bool viewHierarchical)
    {
        var nameColumn = ColumnCreator.Create<EntityId, SharedColumns.Name>();

        return
        [
            viewHierarchical ? ITreeDataGridItemModel<CompositeItemModel<EntityId>, EntityId>.CreateExpanderColumn(nameColumn) : nameColumn,
            ColumnCreator.Create<EntityId, SharedColumns.InstalledDate>(sortDirection: ListSortDirection.Descending),
            ColumnCreator.Create<EntityId, LoadoutColumns.Collections>(),
            ColumnCreator.Create<EntityId, LoadoutColumns.EnabledState>(),
        ];
    }

    private bool _isDisposed;

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            MessageSubject.Dispose();
            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
