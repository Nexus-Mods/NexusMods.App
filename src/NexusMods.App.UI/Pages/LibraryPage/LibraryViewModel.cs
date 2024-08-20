using System.Diagnostics;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Observable = R3.Observable;
using Unit = System.Reactive.Unit;

namespace NexusMods.App.UI.Pages.LibraryPage;

public class LibraryViewModel : APageViewModel<ILibraryViewModel>, ILibraryViewModel
{
    private readonly IConnection _connection;
    private readonly ILibraryService _libraryService;

    [Reactive] public ITreeDataGridSource<LibraryItemModel>? Source { get; set; }
    [Reactive] public bool ViewHierarchical { get; set; } = true;
    private readonly ObservableCollectionExtended<LibraryItemModel> _itemModels = [];

    public Subject<(LibraryItemModel, bool)> ActivationSubject { get; } = new();

    private Dictionary<LibraryItemModel, IDisposable> InstallCommandDisposables { get; set; } = new();
    private Subject<LibraryItemId> InstallLibraryItemSubject { get; } = new();

    public ReactiveCommand<Unit, Unit> SwitchViewCommand { get; }

    private readonly ConnectableObservable<DateTime> _ticker;
    public LibraryViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        LoadoutId loadoutId) : base(windowManager)
    {
        var dataProviders = serviceProvider.GetServices<ILibraryDataProvider>().ToArray();

        _libraryService = serviceProvider.GetRequiredService<ILibraryService>();
        _connection = serviceProvider.GetRequiredService<IConnection>();

        _ticker = Observable
            .Interval(period: TimeSpan.FromSeconds(10), timeProvider: ObservableSystem.DefaultTimeProvider)
            .Select(_ => DateTime.Now)
            .Publish(initialValue: DateTime.Now);

        _ticker.Connect();

        var loadout = Loadout.Load(_connection.Db, loadoutId.Value);
        var game = loadout.InstallationInstance.Game;

        SwitchViewCommand = ReactiveCommand.Create(() =>
        {
            ViewHierarchical = !ViewHierarchical;
        });

        this.WhenActivated(disposables =>
        {
            Disposable.Create(this, static vm =>
            {
                foreach (var kv in vm.InstallCommandDisposables)
                {
                    var (_, disposable) = kv;
                    disposable.Dispose();
                }

                vm.InstallCommandDisposables.Clear();
            });

            ActivationSubject
                .Subscribe(this, static (tuple, vm) =>
                {
                    var (model, isActivated) = tuple;

                    if (isActivated)
                    {
                        model.Ticker = vm._ticker;
                        var disposable = model.InstallCommand.Subscribe(libraryItemId => vm.InstallLibraryItemSubject.OnNext(libraryItemId));
                        var didAdd = vm.InstallCommandDisposables.TryAdd(model, disposable);
                        Debug.Assert(didAdd, "subscription for the model shouldn't exist yet");

                        model.Activate();
                    }
                    else
                    {
                        model.Ticker = null;
                        var didRemove = vm.InstallCommandDisposables.Remove(model, out var disposable);
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

                    return dataProviders
                        .Select(provider => viewHierarchical ? provider.ObserveNestedLibraryItems() : provider.ObserveFlatLibraryItems())
                        .MergeChangeSets()
                        .DisposeMany()
                        .OnUI()
                        .Bind(_itemModels);
                })
                .Switch()
                .Select(_ => CreateSource(_itemModels, createHierarchicalSource: ViewHierarchical))
                .BindTo(this, vm => vm.Source)
                .AddTo(disposables);

            InstallLibraryItemSubject
                .Select(this, static (id, vm) => LibraryItem.Load(vm._connection.Db, id))
                .Where(static item => item.IsValid())
                .SubscribeAwait(async (libraryItem, cancellationToken) =>
                {
                    await using var job = _libraryService.InstallItem(libraryItem, loadout);
                    await job.StartAsync(cancellationToken: cancellationToken);
                    await job.WaitToFinishAsync(cancellationToken: cancellationToken);
                }, awaitOperation: AwaitOperation.Parallel, configureAwait: false)
                .AddTo(disposables);
        });
    }

    private static ITreeDataGridSource<LibraryItemModel> CreateSource(IEnumerable<LibraryItemModel> models, bool createHierarchicalSource)
    {
        if (createHierarchicalSource)
        {
            var source = new HierarchicalTreeDataGridSource<LibraryItemModel>(models);
            AddColumns(source.Columns, viewAsTree: true);
            return source;
        }
        else
        {
            var source = new FlatTreeDataGridSource<LibraryItemModel>(models);
            AddColumns(source.Columns, viewAsTree: false);
            return source;
        }
    }

    private static void AddColumns(ColumnList<LibraryItemModel> columnList, bool viewAsTree)
    {
        var nameColumn = LibraryItemModel.CreateNameColumn();

        columnList.Add(viewAsTree ? LibraryItemModel.CreateExpanderColumn(nameColumn) : nameColumn);
        columnList.Add(LibraryItemModel.CreateVersionColumn());
        columnList.Add(LibraryItemModel.CreateSizeColumn());
        columnList.Add(LibraryItemModel.CreateAddedAtColumn());
        columnList.Add(LibraryItemModel.CreateInstalledAtColumn());
        columnList.Add(LibraryItemModel.CreateInstallColumn());
    }
}
