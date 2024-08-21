using System.Diagnostics;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public class LoadoutViewModel : APageViewModel<ILoadoutViewModel>, ILoadoutViewModel
{
    private readonly IConnection _connection;

    public Subject<(LoadoutItemModel, bool)> ActivationSubject { get; } = new();

    [Reactive] public ITreeDataGridSource<LoadoutItemModel>? Source { get; set; }
    private readonly ObservableCollectionExtended<LoadoutItemModel> _itemModels = [];

    public R3.ReactiveCommand<R3.Unit> SwitchViewCommand { get; }
    [Reactive] public bool ViewHierarchical { get; set; } = true;

    private Dictionary<LoadoutItemModel, IDisposable> ToggleEnableStateCommandDisposables { get; set; } = new();
    private Subject<LoadoutItemId> ToggleEnableSubject { get; } = new();

    public LoadoutViewModel(IWindowManager windowManager, IServiceProvider serviceProvider, LoadoutId loadoutId) : base(windowManager)
    {
        TabTitle = "My Mods (new)";
        TabIcon = IconValues.Collections;

        _connection = serviceProvider.GetRequiredService<IConnection>();
        var dataProviders = serviceProvider.GetServices<ILoadoutDataProvider>().ToArray();

        SwitchViewCommand = new R3.ReactiveCommand<R3.Unit>(_ =>
        {
            ViewHierarchical = !ViewHierarchical;
        });

        this.WhenActivated(disposables =>
        {
            ActivationSubject
                .Subscribe(this, static (tuple, vm) =>
                {
                    var (model, isActivated) = tuple;

                    if (isActivated)
                    {
                        var disposable = model.ToggleEnableStateCommand.Subscribe(vm, static (loadoutItemId, vm) => vm.ToggleEnableSubject.OnNext(loadoutItemId));
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
                .SubscribeWithErrorLogging(s => Source = s)
                // .BindTo(this, vm => vm.Source)
                .AddTo(disposables);

            // TODO: can be optimized with chunking or debounce
            ToggleEnableSubject
                .SubscribeAwait(async (loadoutItemId, cancellationToken) =>
                {
                    using var tx = _connection.BeginTransaction();
                    tx.Add(loadoutItemId, static (tx, db, loadoutItemId) =>
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

    private static ITreeDataGridSource<LoadoutItemModel> CreateSource(IEnumerable<LoadoutItemModel> models, bool createHierarchicalSource)
    {
        if (createHierarchicalSource)
        {
            var source = new HierarchicalTreeDataGridSource<LoadoutItemModel>(models);
            AddColumns(source.Columns, viewAsTree: true);
            return source;
        }
        else
        {
            var source = new FlatTreeDataGridSource<LoadoutItemModel>(models);
            AddColumns(source.Columns, viewAsTree: false);
            return source;
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
