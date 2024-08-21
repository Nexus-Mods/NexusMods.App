using System.Diagnostics;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Observable = R3.Observable;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public class LoadoutViewModel : APageViewModel<ILoadoutViewModel>, ILoadoutViewModel
{
    private readonly IConnection _connection;

    public Subject<(LoadoutItemModel, bool)> ActivationSubject { get; } = new();

    [Reactive] public ITreeDataGridSource<LoadoutItemModel>? Source { get; set; }
    private readonly ObservableCollectionExtended<LoadoutItemModel> _itemModels = [];

    public ReactiveCommand<Unit> SwitchViewCommand { get; }
    [Reactive] public bool ViewHierarchical { get; set; } = true;

    [Reactive] public LoadoutItemModel[] SelectedItemModels { get; private set; } = [];

    private Dictionary<LoadoutItemModel, IDisposable> ToggleEnableStateCommandDisposables { get; set; } = new();
    private Subject<IReadOnlyCollection<LoadoutItemId>> ToggleEnableSubject { get; } = new();

    public ReactiveCommand<Unit> ViewFilesCommand { get; }
    public ReactiveCommand<Unit> RemoveItemCommand { get; }

    public LoadoutViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, LoadoutId loadoutId) : base(windowManager)
    {
        TabTitle = "My Mods (new)";
        TabIcon = IconValues.Collections;

        _connection = serviceProvider.GetRequiredService<IConnection>();
        var dataProviders = serviceProvider.GetServices<ILoadoutDataProvider>().ToArray();

        SwitchViewCommand = new ReactiveCommand<Unit>(_ =>
        {
            ViewHierarchical = !ViewHierarchical;
        });

        var hasSelection = this.WhenAnyValue(vm => vm.SelectedItemModels).ToObservable().Select(arr => arr.Length > 0);

        ViewFilesCommand = hasSelection.ToReactiveCommand<Unit>(_ =>
        {
            // TODO:
        });

        RemoveItemCommand = hasSelection.ToReactiveCommand<Unit>(async (_, cancellationToken) =>
        {
            var ids = SelectedItemModels
                .SelectMany(itemModel => itemModel.GetLoadoutItemIds())
                .ToHashSet();

            using var tx = _connection.BeginTransaction();

            foreach (var id in ids)
            {
                tx.Delete(id, recursive: true);
            }

            await tx.Commit();
        }, awaitOperation: AwaitOperation.Sequential, initialCanExecute: false, configureAwait: false);

        var selectedItemsSerialDisposable = new SerialDisposable();

        this.WhenActivated(disposables =>
        {
            Disposable.Create(this, vm => vm.SelectedItemModels = []);
            Disposable.Create(selectedItemsSerialDisposable, d => d.Disposable = null).AddTo(disposables);

            ActivationSubject
                .Subscribe(this, static (tuple, vm) =>
                {
                    var (model, isActivated) = tuple;

                    if (isActivated)
                    {
                        var disposable = model.ToggleEnableStateCommand.Subscribe(vm, static (ids, vm) => vm.ToggleEnableSubject.OnNext(ids));
                        var didAdd = vm.ToggleEnableStateCommandDisposables.TryAdd(model, disposable);
                        Debug.Assert(didAdd, "subscription for the model shouldn't exist yet");

                        model.Activate();
                    }
                    else
                    {
                        var didRemove = vm.ToggleEnableStateCommandDisposables.Remove(model, out var disposable);
                        Debug.Assert(didRemove, "subscription for the model should exist");
                        disposable?.Dispose();

                        model.Deactivate();
                    }
                })
                .AddTo(disposables);

            this.WhenAnyValue(vm => vm.ViewHierarchical)
                .Select(viewHierarchical =>
                {
                    _itemModels.Clear();

                    var observable = viewHierarchical
                        ? dataProviders.Select(provider => provider.ObserveNestedLoadoutItems()).MergeChangeSets()
                        : ObserveFlatLoadoutItems();

                    return observable
                        .DisposeMany()
                        .OnUI()
                        .Bind(_itemModels);
                })
                .Switch()
                .Select(_ => CreateSource(_itemModels, createHierarchicalSource: ViewHierarchical))
                .Do(tuple =>
                {
                    selectedItemsSerialDisposable.Disposable = tuple.selectionObservable
                        .Subscribe(this, static (arr, vm) => vm.SelectedItemModels = arr);
                })
                .Select(tuple => tuple.source)
                .BindTo(this, vm => vm.Source)
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

    private IObservable<IChangeSet<LoadoutItemModel>> ObserveFlatLoadoutItems()
    {
        return LibraryLinkedLoadoutItem
            .ObserveAll(_connection)
            .Transform(libraryLinkedLoadoutItem => LoadoutDataProviderHelper.ToLoadoutItemModel(_connection, libraryLinkedLoadoutItem))
            .RemoveKey();
    }

    private static (ITreeDataGridSource<LoadoutItemModel> source, Observable<LoadoutItemModel[]> selectionObservable) CreateSource(IEnumerable<LoadoutItemModel> models, bool createHierarchicalSource)
    {
        if (createHierarchicalSource)
        {
            var source = new HierarchicalTreeDataGridSource<LoadoutItemModel>(models);
            var selectionObservable = Observable.FromEventHandler<TreeSelectionModelSelectionChangedEventArgs<LoadoutItemModel>>(
                addHandler: handler => source.RowSelection!.SelectionChanged += handler,
                removeHandler: handler => source.RowSelection!.SelectionChanged -= handler
            ).Select(tuple => tuple.e.SelectedItems.NotNull().ToArray());

            AddColumns(source.Columns, viewAsTree: true);
            return (source, selectionObservable);
        }
        else
        {
            var source = new FlatTreeDataGridSource<LoadoutItemModel>(models);
            var selectionObservable = Observable.FromEventHandler<TreeSelectionModelSelectionChangedEventArgs<LoadoutItemModel>>(
                addHandler: handler => source.RowSelection!.SelectionChanged += handler,
                removeHandler: handler => source.RowSelection!.SelectionChanged -= handler
            ).Select(tuple => tuple.e.SelectedItems.NotNull().ToArray());

            AddColumns(source.Columns, viewAsTree: false);
            return (source, selectionObservable);
        }
    }

    private static void AddColumns(ColumnList<LoadoutItemModel> columnList, bool viewAsTree)
    {
        var nameColumn = LoadoutItemModel.CreateNameColumn();
        columnList.Add(viewAsTree ? LoadoutItemModel.CreateExpanderColumn(nameColumn) : nameColumn);
        // TODO: columnList.Add(LoadoutItemModel.CreateVersionColumn());
        // TODO: columnList.Add(LoadoutItemModel.CreateSizeColumn());
        columnList.Add(LoadoutItemModel.CreateInstalledAtColumn());
        columnList.Add(LoadoutItemModel.CreateToggleEnableColumn());
    }
}
