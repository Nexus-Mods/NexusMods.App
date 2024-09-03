using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.ItemContentsFileTree;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using ObservableCollections;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public class LoadoutViewModel : APageViewModel<ILoadoutViewModel>, ILoadoutViewModel
{
    private readonly IConnection _connection;

    public ReactiveCommand<Unit> SwitchViewCommand { get; }

    public ReactiveCommand<NavigationInformation> ViewFilesCommand { get; }
    public ReactiveCommand<Unit> RemoveItemCommand { get; }

    public LoadoutTreeDataGridAdapter Adapter { get; }

    public LoadoutViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, LoadoutId loadoutId) : base(windowManager)
    {
        Adapter = new LoadoutTreeDataGridAdapter(serviceProvider);

        TabTitle = "My Mods (new)";
        TabIcon = IconValues.Collections;

        _connection = serviceProvider.GetRequiredService<IConnection>();

        SwitchViewCommand = new ReactiveCommand<Unit>(_ => { Adapter.ViewHierarchical.Value = !Adapter.ViewHierarchical.Value; });

        var hasSelection = Adapter.SelectedModels
            .ObserveCountChanged()
            .Select(count => count > 0);

        var viewModFilesArgumentsSubject = new BehaviorSubject<Optional<LoadoutItemGroup.ReadOnly>>(Optional<LoadoutItemGroup.ReadOnly>.None); 

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

        RemoveItemCommand = hasSelection.ToReactiveCommand<Unit>(async (_, cancellationToken) =>
            {
                var ids = Adapter.SelectedModels
                    .SelectMany(itemModel => itemModel.GetLoadoutItemIds())
                    .ToHashSet();

                using var tx = _connection.BeginTransaction();

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
                Adapter.Activate();
                Disposable.Create(Adapter, static adapter => adapter.Deactivate()).AddTo(disposables);

                // TODO: can be optimized with chunking or debounce
                Adapter.MessageSubject
                    .SubscribeAwait(async (message, cancellationToken) =>
                        {
                            using var tx = _connection.BeginTransaction();

                            foreach (var id in message.Ids)
                            {
                                tx.Add(id,
                                    static (tx, db, loadoutItemId) =>
                                    {
                                        var loadoutItem = LoadoutItem.Load(db, loadoutItemId);
                                        if (loadoutItem.IsDisabled)
                                        {
                                            tx.Retract(loadoutItemId, LoadoutItem.Disabled, Null.Instance);
                                        }
                                        else
                                        {
                                            tx.Add(loadoutItemId, LoadoutItem.Disabled, Null.Instance);
                                        }
                                    }
                                );
                            }

                            await tx.Commit();
                        },
                        awaitOperation: AwaitOperation.Parallel,
                        configureAwait: false
                    )
                    .AddTo(disposables);
                
                // Compute the target group for the ViewFilesCommand
                Adapter.SelectedModels.ObserveCountChanged()
                    .Select(this, static (count, vm) => count == 1 ? vm.Adapter.SelectedModels[0] : null)
                    .ObserveOnThreadPool()
                    .Select(_connection,
                        static (model, connection) =>
                        {
                            if (model is null) return Optional<LoadoutItemGroup.ReadOnly>.None;
                            return GetViewModFilesLoadoutItemGroup(model.GetLoadoutItemIds(), connection);
                        }
                    )
                    .ObserveOnUIThreadDispatcher()
                    .Subscribe(viewModFilesArgumentsSubject.OnNext)
                    .AddTo(disposables);
            }
        );
    }


    /// <summary>
    /// Returns the appropriate LoadoutItemGroup of files if the selection contains a LoadoutItemGroup containing files,
    /// if the selection contains multiple LoadoutItemGroups of files, returns None.
    /// </summary>
    private static Optional<LoadoutItemGroup.ReadOnly> GetViewModFilesLoadoutItemGroup(IReadOnlyCollection<LoadoutItemId> loadoutItemIds, IConnection connection)
    {
        var db = connection.Db;
        // Only allow when selecting a single item, or an item with a single child
        if (loadoutItemIds.Count != 1) return Optional<LoadoutItemGroup.ReadOnly>.None;
        var currentGroupId = loadoutItemIds.First();
        
        var groupDatoms = db.Datoms(LoadoutItemGroup.Group, Null.Instance);

        while (true)
        {
            var childDatoms = db.Datoms(LoadoutItem.ParentId, currentGroupId);
            var childGroups = groupDatoms.MergeByEntityId(childDatoms);

            // We have no child groups, check if children are files
            if (childGroups.Count == 0)
            {
                return LoadoutItemWithTargetPath.TryGet(db, currentGroupId, out _) 
                    ? LoadoutItemGroup.Load(db, currentGroupId)
                    : Optional<LoadoutItemGroup.ReadOnly>.None;
            }
            
            // Single child group, check if that group is valid
            if (childGroups.Count == 1)
            {
                currentGroupId = childGroups.First();
                continue;
            }
        
            // We have multiple child groups, return None
            if (childGroups.Count > 1) return Optional<LoadoutItemGroup.ReadOnly>.None;
        }
    }
}

public readonly record struct ToggleEnableState(IReadOnlyCollection<LoadoutItemId> Ids);

public class LoadoutTreeDataGridAdapter : TreeDataGridAdapter<LoadoutItemModel, EntityId>,
    ITreeDataGirdMessageAdapter<ToggleEnableState>
{
    private readonly ILoadoutDataProvider[] _loadoutDataProviders;
    private readonly IConnection _connection;

    public Subject<ToggleEnableState> MessageSubject { get; } = new();
    private readonly Dictionary<LoadoutItemModel, IDisposable> _commandDisposables = new();

    private readonly IDisposable _activationDisposable;

    public LoadoutTreeDataGridAdapter(IServiceProvider serviceProvider)
    {
        _loadoutDataProviders = serviceProvider.GetServices<ILoadoutDataProvider>().ToArray();
        _connection = serviceProvider.GetRequiredService<IConnection>();

        _activationDisposable = this.WhenActivated(static (adapter, disposables) =>
            {
                Disposable.Create(adapter._commandDisposables,
                        static commandDisposables =>
                        {
                            foreach (var kv in commandDisposables)
                            {
                                var (_, disposable) = kv;
                                disposable.Dispose();
                            }

                            commandDisposables.Clear();
                        }
                    )
                    .AddTo(disposables);
            }
        );
    }

    protected override void BeforeModelActivationHook(LoadoutItemModel model)
    {
        var disposable = model.ToggleEnableStateCommand.Subscribe(MessageSubject, static (ids, subject) => { subject.OnNext(new ToggleEnableState(ids)); });

        var didAdd = _commandDisposables.TryAdd(model, disposable);
        Debug.Assert(didAdd, "subscription for the model shouldn't exist yet");

        base.BeforeModelActivationHook(model);
    }

    protected override void BeforeModelDeactivationHook(LoadoutItemModel model)
    {
        var didRemove = _commandDisposables.Remove(model, out var disposable);
        Debug.Assert(didRemove, "subscription for the model should exist");
        disposable?.Dispose();

        base.BeforeModelDeactivationHook(model);
    }

    protected override IObservable<IChangeSet<LoadoutItemModel, EntityId>> GetRootsObservable(bool viewHierarchical)
    {
        var observable = viewHierarchical
            ? _loadoutDataProviders.Select(provider => provider.ObserveNestedLoadoutItems()).MergeChangeSets()
            : ObserveFlatLoadoutItems();

        return observable;
    }

    private IObservable<IChangeSet<LoadoutItemModel, EntityId>> ObserveFlatLoadoutItems()
    {
        return LibraryLinkedLoadoutItem
            .ObserveAll(_connection)
            .Transform(libraryLinkedLoadoutItem => LoadoutDataProviderHelper.ToLoadoutItemModel(_connection, libraryLinkedLoadoutItem));
    }

    protected override IColumn<LoadoutItemModel>[] CreateColumns(bool viewHierarchical)
    {
        var nameColumn = LoadoutItemModel.CreateNameColumn();

        return
        [
            viewHierarchical ? LoadoutItemModel.CreateExpanderColumn(nameColumn) : nameColumn,
            // TODO: LoadoutItemModel.CreateVersionColumn(),
            // TODO: LoadoutItemModel.CreateSizeColumn(),
            LoadoutItemModel.CreateInstalledAtColumn(),
            LoadoutItemModel.CreateToggleEnableColumn(),
        ];
    }

    private bool _isDisposed;

    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _activationDisposable.Dispose();
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
