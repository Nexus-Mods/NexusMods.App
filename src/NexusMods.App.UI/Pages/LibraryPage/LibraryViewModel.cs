using System.Diagnostics;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Platform.Storage;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.CrossPlatform.Process;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Observable = R3.Observable;

namespace NexusMods.App.UI.Pages.LibraryPage;

public class LibraryViewModel : APageViewModel<ILibraryViewModel>, ILibraryViewModel
{
    private readonly IConnection _connection;
    private readonly ILibraryService _libraryService;

    [Reactive] public ITreeDataGridSource<LibraryItemModel>? Source { get; set; }
    [Reactive] public bool ViewHierarchical { get; set; } = true;
    private readonly ObservableCollectionExtended<LibraryItemModel> _itemModels = [];

    public bool IsEmpty { get; [UsedImplicitly] private set; }

    public Subject<(LibraryItemModel, bool)> ActivationSubject { get; } = new();

    [Reactive] public LibraryItemModel[] SelectedItemModels { get; private set; } = [];

    private Dictionary<LibraryItemModel, IDisposable> InstallCommandDisposables { get; set; } = new();
    private Subject<LibraryItemId> InstallLibraryItemSubject { get; } = new();

    public ReactiveCommand<Unit> SwitchViewCommand { get; }

    public ReactiveCommand<Unit> InstallSelectedItemsCommand { get; }

    public ReactiveCommand<Unit> InstallSelectedItemsWithAdvancedInstallerCommand { get; }

    public ReactiveCommand<Unit> OpenFilePickerCommand { get; }

    public ReactiveCommand<Unit> OpenNexusModsCommand { get; }

    [Reactive] public IStorageProvider? StorageProvider { get; set; }

    private readonly ILibraryItemInstaller _advancedInstaller;
    private readonly Loadout.ReadOnly _loadout;

    private readonly ConnectableObservable<DateTime> _ticker;
    public LibraryViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        LoadoutId loadoutId) : base(windowManager)
    {
        _advancedInstaller = serviceProvider.GetRequiredKeyedService<ILibraryItemInstaller>("AdvancedManualInstaller");

        TabTitle = "Library (new)";
        TabIcon = IconValues.ModLibrary;

        var dataProviders = serviceProvider.GetServices<ILibraryDataProvider>().ToArray();

        _libraryService = serviceProvider.GetRequiredService<ILibraryService>();
        _connection = serviceProvider.GetRequiredService<IConnection>();


        _ticker = Observable
            .Interval(period: TimeSpan.FromSeconds(10), timeProvider: ObservableSystem.DefaultTimeProvider)
            .Select(_ => DateTime.Now)
            .Publish(initialValue: DateTime.Now);

        _ticker.Connect();

        _loadout = Loadout.Load(_connection.Db, loadoutId.Value);

        SwitchViewCommand = new ReactiveCommand<Unit>(_ =>
        {
            ViewHierarchical = !ViewHierarchical;
        });

        var hasSelection = this.WhenAnyValue(vm => vm.SelectedItemModels)
            .ToObservable()
            .Select(arr => arr.Length > 0);

        InstallSelectedItemsCommand = hasSelection.ToReactiveCommand<Unit>(
            executeAsync: (_, cancellationToken) => InstallSelectedItems(useAdvancedInstaller: false, cancellationToken),
            awaitOperation: AwaitOperation.Parallel,
            initialCanExecute: false,
            configureAwait: false
        );

        InstallSelectedItemsWithAdvancedInstallerCommand = hasSelection.ToReactiveCommand<Unit>(
            executeAsync: (_, cancellationToken) => InstallSelectedItems(useAdvancedInstaller: true, cancellationToken),
            awaitOperation: AwaitOperation.Parallel,
            initialCanExecute: false,
            configureAwait: false
        );

        var canUseFilePicker = this.WhenAnyValue(vm => vm.StorageProvider)
            .ToObservable()
            .WhereNotNull()
            .Select(x => x.CanOpen);

        OpenFilePickerCommand = canUseFilePicker.ToReactiveCommand<Unit>(
            executeAsync: (_, cancellationToken) => AddFilesFromDisk(StorageProvider!, cancellationToken),
            awaitOperation: AwaitOperation.Parallel,
            initialCanExecute: true,
            configureAwait: false
        );

        var osInterop = serviceProvider.GetRequiredService<IOSInterop>();
        var gameDomain = _loadout.InstallationInstance.Game.Domain;
        var gameUri = new Uri($"https://www.nexusmods.com/{gameDomain}");

        OpenNexusModsCommand = new ReactiveCommand<Unit>(
            executeAsync: async (_, cancellationToken) => await osInterop.OpenUrl(gameUri, cancellationToken: cancellationToken),
            awaitOperation: AwaitOperation.Parallel,
            configureAwait: false
        );

        var selectedItemsSerialDisposable = new SerialDisposable();

        this.WhenActivated(disposables =>
        {
            Disposable.Create(this, static vm => vm.StorageProvider = null);

            this.WhenAnyValue(vm => vm._itemModels.Count)
                .Select(count => count == 0)
                .BindToVM(this, vm => vm.IsEmpty)
                .AddTo(disposables);

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
                        var disposable = model.InstallCommand.Subscribe(vm, static (libraryItemId, vm) => vm.InstallLibraryItemSubject.OnNext(libraryItemId));
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
                .Do(tuple =>
                {
                    selectedItemsSerialDisposable.Disposable = tuple.selectionObservable
                        .Subscribe(this, static (arr, vm) => vm.SelectedItemModels = arr);
                })
                .Select(tuple => tuple.source)
                .BindTo(this, vm => vm.Source)
                .AddTo(disposables);

            InstallLibraryItemSubject
                .Select(this, static (id, vm) => LibraryItem.Load(vm._connection.Db, id))
                .Where(static item => item.IsValid())
                .SubscribeAwait(
                    onNextAsync: (libraryItem, cancellationToken) => InstallLibraryItem(libraryItem, _loadout, cancellationToken),
                    awaitOperation: AwaitOperation.Parallel,
                    configureAwait: false
                ).AddTo(disposables);
        });
    }

    private async ValueTask InstallSelectedItems(bool useAdvancedInstaller, CancellationToken cancellationToken)
    {
        // TODO: get correct IDs
        var db = _connection.Db;
        var items = SelectedItemModels
            .Select(model => model.LibraryItemId)
            .Where(x => x.HasValue)
            .Select(x => x.Value)
            .Distinct()
            .Select(id => LibraryItem.Load(db, id))
            .ToArray();

        await Parallel.ForAsync(
            fromInclusive: 0,
            toExclusive: items.Length,
            body: (i, innerCancellationToken) => InstallLibraryItem(items[i], _loadout, innerCancellationToken, useAdvancedInstaller),
            cancellationToken: cancellationToken
        );
    }

    private async ValueTask InstallLibraryItem(
        LibraryItem.ReadOnly libraryItem,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken,
        bool useAdvancedInstaller = false)
    {
        await using var job = _libraryService.InstallItem(libraryItem, loadout, useAdvancedInstaller ? _advancedInstaller : null);
        await job.StartAsync(cancellationToken: cancellationToken);
        await job.WaitToFinishAsync(cancellationToken: cancellationToken);
    }

    private async ValueTask AddFilesFromDisk(IStorageProvider storageProvider, CancellationToken cancellationToken)
    {
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            Title = Language.LoadoutGridView_AddMod_FilePicker_Title,
            FileTypeFilter =
            [
                // TODO: fetch from some service
                new FilePickerFileType(Language.LoadoutGridView_AddMod_FileType_Archive)
                {
                    Patterns = ["*.zip", "*.7z", "*.rar"],
                },
            ],
        });

        var paths = files
            .Select(file => file.TryGetLocalPath())
            .NotNull()
            .Select(path => FileSystem.Shared.FromUnsanitizedFullPath(path))
            .Where(path => path.FileExists)
            .ToArray();

        await Parallel.ForAsync(
            fromInclusive: 0,
            toExclusive: paths.Length,
            body: async (i, innerCancellationToken) =>
            {
                await using var job = _libraryService.AddLocalFile(paths[i]);
                await job.StartAsync(cancellationToken: innerCancellationToken);
                await job.WaitToFinishAsync(cancellationToken: innerCancellationToken);
            },
            cancellationToken: cancellationToken
        );
    }

    private static (ITreeDataGridSource<LibraryItemModel> source, Observable<LibraryItemModel[]> selectionObservable) CreateSource(IEnumerable<LibraryItemModel> models, bool createHierarchicalSource)
    {
        if (createHierarchicalSource)
        {
            var source = new HierarchicalTreeDataGridSource<LibraryItemModel>(models);
            var selectionObservable = Observable.FromEventHandler<TreeSelectionModelSelectionChangedEventArgs<LibraryItemModel>>(
                addHandler: handler => source.RowSelection!.SelectionChanged += handler,
                removeHandler: handler => source.RowSelection!.SelectionChanged -= handler
            ).Select(tuple => tuple.e.SelectedItems.NotNull().ToArray());

            AddColumns(source.Columns, viewAsTree: true);
            return (source, selectionObservable);
        }
        else
        {
            var source = new FlatTreeDataGridSource<LibraryItemModel>(models);
            var selectionObservable = Observable.FromEventHandler<TreeSelectionModelSelectionChangedEventArgs<LibraryItemModel>>(
                addHandler: handler => source.RowSelection!.SelectionChanged += handler,
                removeHandler: handler => source.RowSelection!.SelectionChanged -= handler
            ).Select(tuple => tuple.e.SelectedItems.NotNull().ToArray());

            AddColumns(source.Columns, viewAsTree: false);
            return (source, selectionObservable);
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
