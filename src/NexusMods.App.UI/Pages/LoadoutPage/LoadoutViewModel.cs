using System.ComponentModel;
using System.Reactive.Linq;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Controls.Trees;
using NexusMods.App.UI.Pages.ItemContentsFileTree;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using ObservableCollections;
using OneOf;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public class LoadoutViewModel : APageViewModel<ILoadoutViewModel>, ILoadoutViewModel
{
    public ReactiveCommand<Unit> SwitchViewCommand { get; }
    public string EmptyStateTitleText { get; }
    public ReactiveCommand<NavigationInformation> ViewFilesCommand { get; }
    public ReactiveCommand<NavigationInformation> ViewLibraryCommand { get; }
    public ReactiveCommand<Unit> RemoveItemCommand { get; }
    public ReactiveCommand<Unit> CollectionToggleCommand { get; }

    public LoadoutTreeDataGridAdapter Adapter { get; }
    
    [Reactive] public bool IsCollection { get; private set; } 
    [Reactive] public bool IsCollectionEnabled { get; private set; }

    public LoadoutViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, LoadoutId loadoutId, Optional<LoadoutItemGroupId> collectionGroupId = default) : base(windowManager)
    {
        var loadoutFilter = new LoadoutFilter
        {
            LoadoutId = loadoutId,
            CollectionGroupId = collectionGroupId,
        };

        Adapter = new LoadoutTreeDataGridAdapter(serviceProvider, loadoutFilter);

        var connection = serviceProvider.GetRequiredService<IConnection>();

        if (collectionGroupId.HasValue)
        {
            var collectionGroup = LoadoutItem.Load(connection.Db, collectionGroupId.Value);
            TabTitle = collectionGroup.Name;
            TabIcon = IconValues.CollectionsOutline;
            IsCollection = true;
            CollectionToggleCommand = new ReactiveCommand<Unit>(
                async (_, _) => await ToggleCollectionGroup(collectionGroupId, !IsCollectionEnabled, connection), 
                configureAwait: false
            );
        }
        else
        {
            TabTitle = Language.LoadoutViewPageTitle;
            TabIcon = IconValues.FormatAlignJustify;
            CollectionToggleCommand = new ReactiveCommand<Unit>(_ => { });
        }

        SwitchViewCommand = new ReactiveCommand<Unit>(_ => { Adapter.ViewHierarchical.Value = !Adapter.ViewHierarchical.Value; });

        var hasSelection = Adapter.SelectedModels
            .ObserveCountChanged()
            .Select(count => count > 0);
            
        

        var viewModFilesArgumentsSubject = new BehaviorSubject<Optional<LoadoutItemGroup.ReadOnly>>(Optional<LoadoutItemGroup.ReadOnly>.None); 
        
        var loadout = Loadout.Load(connection.Db, loadoutId);
        EmptyStateTitleText = string.Format(Language.LoadoutGridViewModel_EmptyModlistTitleString, loadout.InstallationInstance.Game.Name);
        ViewLibraryCommand = new ReactiveCommand<NavigationInformation>(info =>
            {
                var pageData = new PageData
                {
                    FactoryId = LibraryPageFactory.StaticId,
                    Context = new LibraryPageContext
                    {
                        LoadoutId = loadoutId,
                    },
                };
                var workspaceController = GetWorkspaceController();
                var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
                workspaceController.OpenPage(workspaceController.ActiveWorkspaceId, pageData, behavior);
            }
        );

        ViewFilesCommand = viewModFilesArgumentsSubject
            .Select(viewModFilesArguments => viewModFilesArguments.HasValue)
            .ToReactiveCommand<NavigationInformation>( info =>
                {
                    var group = viewModFilesArgumentsSubject.Value;
                    if (!group.HasValue) return;

                    var isReadonly = group.Value.AsLoadoutItem()
                        .GetThisAndParents()
                        .Any(item => IsRequired(item.LoadoutItemId, connection));

                    var pageData = new PageData
                    {
                        FactoryId = ItemContentsFileTreePageFactory.StaticId,
                        Context = new ItemContentsFileTreePageContext
                        {
                            GroupId = group.Value.Id,
                            IsReadOnly = isReadonly,
                        },
                    };
                    var workspaceController = GetWorkspaceController();
                    var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
                    workspaceController.OpenPage(workspaceController.ActiveWorkspaceId, pageData, behavior);
                },
                false
            );
        
        var hasValidRemoveSelection = Adapter.SelectedModels
            .ObserveChanged()
            .SelectMany(_ =>
            {
                var observables = Adapter.SelectedModels.Select(model => 
                    model.GetObservable<LoadoutComponents.LockedEnabledState>(LoadoutColumns.EnabledState.LockedEnabledStateComponentKey));
                
                return R3.Observable.CombineLatest(observables)
                    // if all items are readonly, or list is empty, no valid selection
                    .Select(list => !list.All(x => x.HasValue));
            });
            
        
        RemoveItemCommand = hasValidRemoveSelection
            .ToReactiveCommand<Unit>(async (_, _) =>
            {
                var ids = Adapter.SelectedModels
                    .SelectMany(static itemModel => GetLoadoutItemIds(itemModel))
                    .ToHashSet()
                    .Where(id => !IsRequired(id, connection))
                    .ToArray();

                if (ids.Length == 0) return;
                using var tx = connection.BeginTransaction();

                foreach (var id in ids)
                {
                    tx.Delete(id, recursive: true);
                }

                await tx.Commit();
            },
            awaitOperation: AwaitOperation.Sequential,
            initialCanExecute: false,
            configureAwait: false
        );

        this.WhenActivated(disposables =>
        {
            Adapter.Activate().AddTo(disposables);

            Adapter.MessageSubject.SubscribeAwait(async (message, _) =>
            {   
                // Toggle item state
                if (message.IsT0){
                    await ToggleItemEnabledState(message.AsT0.Ids, connection);
                    return;
                }

                // Open collection
                if (message.IsT1)
                {
                    var data = message.AsT1;
                    OpenItemCollectionPage(data.Ids, data.NavigationInformation, loadoutId, GetWorkspaceController(), connection);
                    return;
                }

            }, awaitOperation: AwaitOperation.Parallel, configureAwait: false).AddTo(disposables);

            // Compute the target group for the ViewFilesCommand
            Adapter.SelectedModels.ObserveCountChanged(notifyCurrentCount: true)
                .Select(this, static (count, vm) => count == 1 ? vm.Adapter.SelectedModels.First() : null)
                .ObserveOnThreadPool()
                .Select(connection, static (model, connection) =>
                {
                    if (model is null) return Optional<LoadoutItemGroup.ReadOnly>.None;
                    return LoadoutItemGroupFileTreeViewModel.GetViewModFilesLoadoutItemGroup(GetLoadoutItemIds(model).ToArray(), connection);
                })
                .ObserveOnUIThreadDispatcher()
                .Subscribe(viewModFilesArgumentsSubject.OnNext)
                .AddTo(disposables);
            
            if (collectionGroupId.HasValue)
            {
                LoadoutItem.Observe(connection, collectionGroupId.Value)
                    .Select(item => item.IsEnabled())
                    .OnUI()
                    .Subscribe(isEnabled => IsCollectionEnabled = isEnabled)
                    .AddTo(disposables);
            }
        });
    }

    internal static async Task ToggleItemEnabledState(LoadoutItemId[] ids, IConnection connection)
    {
        var toggleableItems = ids
            .Select(loadoutItemId => LoadoutItem.Load(connection.Db, loadoutItemId))
            // Exclude collection required items
            .Where(item => !IsRequired(item.Id, connection))
            // Exclude items that are part of a collection that is disabled
            .Where(item => !(item.Parent.TryGetAsCollectionGroup(out var collectionGroup)
                             && collectionGroup.AsLoadoutItemGroup().AsLoadoutItem().IsDisabled)
            )
            .ToArray();

        if (toggleableItems.Length == 0) return;

        // We only enable if all items are disabled, otherwise we disable
        var shouldEnable = toggleableItems.All(loadoutItem => loadoutItem.IsDisabled);

        using var tx = connection.BeginTransaction();

        foreach (var id in toggleableItems)
        {
            if (shouldEnable)
            {
                tx.Retract(id, LoadoutItem.Disabled, Null.Instance);
            }
            else
            {
                tx.Add(id, LoadoutItem.Disabled, Null.Instance);
            }
        }

        await tx.Commit();
    }

    internal static void OpenItemCollectionPage(
        LoadoutItemId[] ids,
        NavigationInformation navInfo,
        LoadoutId loadoutId,
        IWorkspaceController workspaceController,
        IConnection connection)
    {
        if (ids.Length == 0) return;

        // Open the collection page for the first item
        var firstItemId = ids.First();

        var parentGroup = LoadoutItem.Load(connection.Db, firstItemId).Parent;
        if (!parentGroup.TryGetAsCollectionGroup(out var collectionGroup)) return;

        if (collectionGroup.TryGetAsNexusCollectionLoadoutGroup(out var nexusCollectionGroup))
        {
            var nexusCollPageData = new PageData
            {
                FactoryId = CollectionLoadoutPageFactory.StaticId,
                Context = new CollectionLoadoutPageContext
                {
                    LoadoutId = loadoutId,
                    GroupId = nexusCollectionGroup.Id,
                },
            };
            var nexusPageBehavior = workspaceController.GetOpenPageBehavior(nexusCollPageData, navInfo);
            workspaceController.OpenPage(workspaceController.ActiveWorkspaceId, nexusCollPageData, nexusPageBehavior);

            return;
        }

        var pageData = new PageData
        {
            FactoryId = LoadoutPageFactory.StaticId,
            Context = new LoadoutPageContext
            {
                LoadoutId = loadoutId,
                GroupScope = collectionGroup.AsLoadoutItemGroup().LoadoutItemGroupId,
            },
        };
        var behavior = workspaceController.GetOpenPageBehavior(pageData, navInfo);
        workspaceController.OpenPage(workspaceController.ActiveWorkspaceId, pageData, behavior);

        return;
    }

    private static async Task ToggleCollectionGroup(Optional<LoadoutItemGroupId> collectionGroupId, bool shouldEnable, IConnection connection)
    {
        if (!collectionGroupId.HasValue) return;
        using var tx = connection.BeginTransaction();
        if (shouldEnable)
        {
            tx.Retract(collectionGroupId.Value, LoadoutItem.Disabled, Null.Instance);
        }
        else
        {
            tx.Add(collectionGroupId.Value, LoadoutItem.Disabled, Null.Instance);
        }
        await tx.Commit();
    }

    private static IEnumerable<LoadoutItemId> GetLoadoutItemIds(CompositeItemModel<EntityId> itemModel)
    {
        return itemModel.Get<LoadoutComponents.LoadoutItemIds>(LoadoutColumns.EnabledState.LoadoutItemIdsComponentKey).ItemIds;
    }

    private static bool IsRequired(LoadoutItemId id, IConnection connection)
    {
        return NexusCollectionItemLoadoutGroup.IsRequired.TryGetValue(LoadoutItem.Load(connection.Db, id), out var isRequired) && isRequired;
    }
}

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
                var (self, itemModel, component) = tuple;
                var ids = GetLoadoutItemIds(itemModel).ToArray();

                self.MessageSubject.OnNext(new ToggleEnableStateMessage(ids));
            })
        );

        model.SubscribeToComponentAndTrack<LoadoutComponents.ParentCollectionDisabled, LoadoutTreeDataGridAdapter>(
            key: LoadoutColumns.EnabledState.ParentCollectionDisabledComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.ButtonCommand.Subscribe((self, itemModel, component), static (navInfo, tuple) =>
            {
                var (self, itemModel, component) = tuple;
                var ids = GetLoadoutItemIds(itemModel).ToArray();

                self.MessageSubject.OnNext(new OpenCollectionMessage(ids, navInfo));
            })
        );

        model.SubscribeToComponentAndTrack<LoadoutComponents.LockedEnabledState, LoadoutTreeDataGridAdapter>(
            key: LoadoutColumns.EnabledState.LockedEnabledStateComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.ButtonCommand.Subscribe((self, itemModel, component), static (navInfo, tuple) =>
            {
                var (self, itemModel, component) = tuple;
                var ids = GetLoadoutItemIds(itemModel).ToArray();

                self.MessageSubject.OnNext(new OpenCollectionMessage(ids, navInfo));
            })
        );

        model.SubscribeToComponentAndTrack<LoadoutComponents.MixLockedAndParentDisabled, LoadoutTreeDataGridAdapter>(
            key: LoadoutColumns.EnabledState.MixLockedAndParentDisabledComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.ButtonCommand.Subscribe((self, itemModel, component), static (navInfo, tuple) =>
            {
                var (self, itemModel, component) = tuple;
                var ids = GetLoadoutItemIds(itemModel).ToArray();

                self.MessageSubject.OnNext(new OpenCollectionMessage(ids, navInfo));
            })
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
