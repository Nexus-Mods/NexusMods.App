using System.Diagnostics;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Controls.Trees;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.ItemContentsFileTree;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
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
        var ticker = Observable
            .Interval(period: TimeSpan.FromSeconds(30), timeProvider: ObservableSystem.DefaultTimeProvider)
            .ObserveOnUIThreadDispatcher()
            .Select(_ => DateTime.Now)
            .Publish(initialValue: DateTime.Now);

        var loadoutFilter = new LoadoutFilter
        {
            LoadoutId = loadoutId,
        };

        Adapter = new LoadoutTreeDataGridAdapter(serviceProvider, ticker, loadoutFilter);

        TabTitle = "My Mods (new)";
        TabIcon = IconValues.Collections;

        _connection = serviceProvider.GetRequiredService<IConnection>();

        SwitchViewCommand = new ReactiveCommand<Unit>(_ => { Adapter.ViewHierarchical.Value = !Adapter.ViewHierarchical.Value; });
        ticker.Connect();

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
                Adapter.MessageSubject.SubscribeAwait(async (message, cancellationToken) =>
                {
                    using var tx = _connection.BeginTransaction();

                    foreach (var (loadoutItemId, shouldEnable) in message.Ids)
                    {
                        if (shouldEnable)
                        {
                            tx.Retract(loadoutItemId, LoadoutItem.Disabled, Null.Instance);
                        } else
                        {
                            tx.Add(loadoutItemId, LoadoutItem.Disabled, Null.Instance);
                        }
                    }

                    await tx.Commit();
                },
                awaitOperation: AwaitOperation.Parallel,
                configureAwait: false).AddTo(disposables);

                // Compute the target group for the ViewFilesCommand
                Adapter.SelectedModels.ObserveCountChanged()
                    .Select(this, static (count, vm) => count == 1 ? vm.Adapter.SelectedModels.First() : null)
                    .ObserveOnThreadPool()
                    .Select(_connection,
                        static (model, connection) =>
                        {
                            if (model is null) return Optional<LoadoutItemGroup.ReadOnly>.None;
                            return LoadoutItemGroupFileTreeViewModel.GetViewModFilesLoadoutItemGroup(model.GetLoadoutItemIds(), connection);
                        }
                    )
                    .ObserveOnUIThreadDispatcher()
                    .Subscribe(viewModFilesArgumentsSubject.OnNext)
                    .AddTo(disposables);
            }
        );
    }
}

public readonly record struct ToggleEnableState(IReadOnlyCollection<(LoadoutItemId Id, bool ShouldEnable)> Ids);

public class LoadoutTreeDataGridAdapter : TreeDataGridAdapter<LoadoutItemModel, EntityId>,
    ITreeDataGirdMessageAdapter<ToggleEnableState>
{
    private readonly ILoadoutDataProvider[] _loadoutDataProviders;
    private readonly ConnectableObservable<DateTime> _ticker;
    private readonly IConnection _connection;
    private readonly LoadoutFilter _loadoutFilter;

    public Subject<ToggleEnableState> MessageSubject { get; } = new();
    private readonly Dictionary<LoadoutItemModel, IDisposable> _commandDisposables = new();

    private readonly IDisposable _activationDisposable;
    public LoadoutTreeDataGridAdapter(IServiceProvider serviceProvider, ConnectableObservable<DateTime> ticker, LoadoutFilter loadoutFilter)
    {
        _loadoutFilter = loadoutFilter;
        _ticker = ticker;

        _loadoutDataProviders = serviceProvider.GetServices<ILoadoutDataProvider>().ToArray();
        _connection = serviceProvider.GetRequiredService<IConnection>();

        _activationDisposable = this.WhenActivated(static (adapter, disposables) =>
        {
            Disposable.Create(adapter._commandDisposables,static commandDisposables =>
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

    protected override void BeforeModelActivationHook(LoadoutItemModel model)
    {
        var disposable = model.ToggleEnableStateCommand.Subscribe(MessageSubject, static (ids, subject) => { subject.OnNext(new ToggleEnableState(ids)); });
        model.Ticker = _ticker;

        var didAdd = _commandDisposables.TryAdd(model, disposable);
        Debug.Assert(didAdd, "subscription for the model shouldn't exist yet");

        base.BeforeModelActivationHook(model);
    }

    protected override void BeforeModelDeactivationHook(LoadoutItemModel model)
    {
        model.Ticker = null;

        var didRemove = _commandDisposables.Remove(model, out var disposable);
        Debug.Assert(didRemove, "subscription for the model should exist");
        disposable?.Dispose();

        base.BeforeModelDeactivationHook(model);
    }

    protected override IObservable<IChangeSet<LoadoutItemModel, EntityId>> GetRootsObservable(bool viewHierarchical)
    {
        var observable = viewHierarchical
            ? _loadoutDataProviders.Select(provider => provider.ObserveNestedLoadoutItems(_loadoutFilter)).MergeChangeSets()
            : ObserveFlatLoadoutItems();

        return observable;
    }

    private IObservable<IChangeSet<LoadoutItemModel, EntityId>> ObserveFlatLoadoutItems()
    {
        return LibraryLinkedLoadoutItem
            .ObserveAll(_connection)
            .Filter(item => LoadoutItem.LoadoutId.Get(item).Equals(_loadoutFilter.LoadoutId))
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
                Disposable.Dispose(_activationDisposable, MessageSubject);
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
