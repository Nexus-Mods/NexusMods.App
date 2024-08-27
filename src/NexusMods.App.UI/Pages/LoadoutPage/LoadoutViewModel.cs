using System.Diagnostics;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
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

            // TODO: can be optimized with chunking or debounce
            Adapter.MessageSubject
                .SubscribeAwait(async (message, cancellationToken) =>
                {
                    using var tx = _connection.BeginTransaction();

                    foreach (var id in message.Ids)
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

public readonly record struct ToggleEnableState(IReadOnlyCollection<LoadoutItemId> Ids);

public class LoadoutTreeDataGridAdapter : TreeDataGridAdapter<LoadoutItemModel>,
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
            Disposable.Create(adapter._commandDisposables, static commandDisposables =>
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
        var disposable = model.ToggleEnableStateCommand.Subscribe(MessageSubject, static (ids, subject) =>
        {
            subject.OnNext(new ToggleEnableState(ids));
        });

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
