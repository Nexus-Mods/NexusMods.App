using System.Diagnostics;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls;
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

    private Dictionary<LoadoutItemModel, IDisposable> ToggleEnableStateCommandDisposables { get; set; } = new();
    private Subject<IReadOnlyCollection<LoadoutItemId>> ToggleEnableSubject { get; } = new();

    public ReactiveCommand<Unit> ViewFilesCommand { get; }
    public ReactiveCommand<Unit> RemoveItemCommand { get; }

    public LoadoutTreeDataGridAdapter Adapter { get; }

    public LoadoutViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, LoadoutId loadoutId) : base(windowManager)
    {
        Adapter = new LoadoutTreeDataGridAdapter(serviceProvider);

        TabTitle = "My Mods (new)";
        TabIcon = IconValues.Collections;

        _connection = serviceProvider.GetRequiredService<IConnection>();

        SwitchViewCommand = new ReactiveCommand<Unit>(_ =>
        {
            Adapter.ViewHierarchical.Value = !Adapter.ViewHierarchical.Value;
        });

        var hasSelection = Adapter.SelectedModels
            .ObserveCountChanged()
            .Select(count => count > 0);

        ViewFilesCommand = hasSelection.ToReactiveCommand<Unit>(_ =>
        {
            // TODO:
        });

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
        }, awaitOperation: AwaitOperation.Sequential, initialCanExecute: false, configureAwait: false);

        this.WhenActivated(disposables =>
        {
            Adapter.Activate();
            Disposable.Create(Adapter, static adapter => adapter.Deactivate()).AddTo(disposables);

            Disposable.Create(this, static vm =>
            {
                foreach (var kv in vm.ToggleEnableStateCommandDisposables)
                {
                    var (_, disposable) = kv;
                    disposable.Dispose();
                }

                vm.ToggleEnableStateCommandDisposables.Clear();
            }).AddTo(disposables);

            Adapter.ModelActivationSubject
                .Subscribe(this, static (tuple, vm) =>
                {
                    var (model, isActivated) = tuple;

                    if (isActivated)
                    {
                        var disposable = model.ToggleEnableStateCommand.Subscribe(vm, static (ids, vm) => vm.ToggleEnableSubject.OnNext(ids));
                        var didAdd = vm.ToggleEnableStateCommandDisposables.TryAdd(model, disposable);
                        Debug.Assert(didAdd, "subscription for the model shouldn't exist yet");
                    }
                    else
                    {
                        var didRemove = vm.ToggleEnableStateCommandDisposables.Remove(model, out var disposable);
                        Debug.Assert(didRemove, "subscription for the model should exist");
                        disposable?.Dispose();
                    }
                })
                .AddTo(disposables);

            // TODO: can be optimized with chunking or debounce
            ToggleEnableSubject
                .SubscribeAwait(async (ids, cancellationToken) =>
                {
                    using var tx = _connection.BeginTransaction();

                    foreach (var id in ids)
                    {
                        tx.Add(id, static (tx, db, loadoutItemId) =>
                        {
                            var loadoutItem = LoadoutItem.Load(db, loadoutItemId);
                            if (loadoutItem.IsDisabled)
                            {
                                tx.Retract(loadoutItemId, LoadoutItem.Disabled, Null.Instance);
                            } else
                            {
                                tx.Add(loadoutItemId, LoadoutItem.Disabled, Null.Instance);
                            }
                        });
                    }

                    await tx.Commit();
                }, awaitOperation: AwaitOperation.Parallel, configureAwait: false)
                .AddTo(disposables);
        });
    }
}

public class LoadoutTreeDataGridAdapter : TreeDataGridAdapter<LoadoutItemModel>
{
    private readonly ILoadoutDataProvider[] _loadoutDataProviders;
    private readonly IConnection _connection;

    public LoadoutTreeDataGridAdapter(IServiceProvider serviceProvider)
    {
        _loadoutDataProviders = serviceProvider.GetServices<ILoadoutDataProvider>().ToArray();
        _connection = serviceProvider.GetRequiredService<IConnection>();
    }

    protected override IObservable<IChangeSet<LoadoutItemModel>> GetRootsObservable(bool viewHierarchical)
    {
        var observable = viewHierarchical
            ? _loadoutDataProviders.Select(provider => provider.ObserveNestedLoadoutItems()).MergeChangeSets()
            : ObserveFlatLoadoutItems();

        return observable;
    }

    private IObservable<IChangeSet<LoadoutItemModel>> ObserveFlatLoadoutItems()
    {
        return LibraryLinkedLoadoutItem
            .ObserveAll(_connection)
            .Transform(libraryLinkedLoadoutItem => LoadoutDataProviderHelper.ToLoadoutItemModel(_connection, libraryLinkedLoadoutItem))
            .RemoveKey();
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
}
