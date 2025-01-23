using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.UI.Extensions;
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

                    var pageData = new PageData
                    {
                        FactoryId = ItemContentsFileTreePageFactory.StaticId,
                        Context = new ItemContentsFileTreePageContext
                        {
                            GroupId = group.Value.Id,
                        },
                    };
                    var workspaceController = GetWorkspaceController();
                    var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
                    workspaceController.OpenPage(workspaceController.ActiveWorkspaceId, pageData, behavior);
                },
                false
            );

        RemoveItemCommand = hasSelection.ToReactiveCommand<Unit>(async (_, _) =>
            {
                var ids = Adapter.SelectedModels
                    .SelectMany(static itemModel => GetLoadoutItemIds(itemModel))
                    .ToHashSet();

                if (ids.Count == 0) return;
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
                if (message.Ids.Length == 0) return;
                using var tx = connection.BeginTransaction();

                foreach (var loadoutItemId in message.Ids)
                {
                    if (message.ShouldEnable)
                    {
                        tx.Retract(loadoutItemId, LoadoutItem.Disabled, Null.Instance);
                    }
                    else
                    {
                        tx.Add(loadoutItemId, LoadoutItem.Disabled, Null.Instance);
                    }
                }

                await tx.Commit();
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
        return itemModel.Get<LoadoutComponents.IsEnabled>(LoadoutColumns.IsEnabled.ComponentKey).ItemIds;
    }
}

public readonly record struct ToggleEnableState(LoadoutItemId[] Ids, bool ShouldEnable);

public class LoadoutTreeDataGridAdapter :
    TreeDataGridAdapter<CompositeItemModel<EntityId>, EntityId>,
    ITreeDataGirdMessageAdapter<ToggleEnableState>
{
    public Subject<ToggleEnableState> MessageSubject { get; } = new();

    private readonly ILoadoutDataProvider[] _loadoutDataProviders;
    private readonly LoadoutFilter _loadoutFilter;

    private readonly IDisposable _activationDisposable;
    private readonly Dictionary<CompositeItemModel<EntityId>, IDisposable> _commandDisposables = new();
    public LoadoutTreeDataGridAdapter(IServiceProvider serviceProvider, LoadoutFilter loadoutFilter)
    {
        _loadoutDataProviders = serviceProvider.GetServices<ILoadoutDataProvider>().ToArray();
        _loadoutFilter = loadoutFilter;

        _activationDisposable = this.WhenActivated(static (self, disposables) =>
        {
            Disposable.Create(self._commandDisposables,static commandDisposables =>
            {
                foreach (var kv in commandDisposables)
                {
                    var (_, disposable) = kv;
                    disposable.Dispose();
                }

                commandDisposables.Clear();
            }).AddTo(disposables);
        });
    }

    protected override IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> GetRootsObservable(bool viewHierarchical)
    {
        return _loadoutDataProviders.Select(x => x.ObserveLoadoutItems(_loadoutFilter)).MergeChangeSets();
    }

    protected override void BeforeModelActivationHook(CompositeItemModel<EntityId> model)
    {
        base.BeforeModelActivationHook(model);

        var disposable = model.SubscribeToComponent<LoadoutComponents.IsEnabled, LoadoutTreeDataGridAdapter>(
            key: LoadoutColumns.IsEnabled.ComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandToggle.Subscribe((self, itemModel, component), static (_, tuple) =>
            {
                var (self, itemModel, component) = tuple;
                var isEnabled = component.Value.Value;
                var ids = component.ItemIds.ToArray();
                var shouldEnable = !isEnabled ?? false;

                self.MessageSubject.OnNext(new ToggleEnableState(ids, shouldEnable));
            })
        );

        var didAdd = _commandDisposables.TryAdd(model, disposable);
        Debug.Assert(didAdd, "subscription for the model shouldn't exist yet");
    }

    protected override void BeforeModelDeactivationHook(CompositeItemModel<EntityId> model)
    {
        base.BeforeModelDeactivationHook(model);

        var didRemove = _commandDisposables.Remove(model, out var disposable);
        Debug.Assert(didRemove, "subscription for the model should exist");
        disposable?.Dispose();
    }

    protected override IColumn<CompositeItemModel<EntityId>>[] CreateColumns(bool viewHierarchical)
    {
        var nameColumn = ColumnCreator.Create<EntityId, SharedColumns.Name>(sortDirection: ListSortDirection.Ascending);

        return
        [
            viewHierarchical ? ITreeDataGridItemModel<CompositeItemModel<EntityId>, EntityId>.CreateExpanderColumn(nameColumn) : nameColumn,
            ColumnCreator.Create<EntityId, SharedColumns.InstalledDate>(),
            ColumnCreator.Create<EntityId, LoadoutColumns.IsEnabled>(),
        ];
    }

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            Disposable.Dispose(_activationDisposable, MessageSubject);
            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
