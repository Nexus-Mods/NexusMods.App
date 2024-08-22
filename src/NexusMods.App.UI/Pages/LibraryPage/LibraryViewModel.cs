using System.Diagnostics;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Platform.Storage;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.CrossPlatform.Process;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using ObservableCollections;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Observable = R3.Observable;

namespace NexusMods.App.UI.Pages.LibraryPage;

public class LibraryViewModel : APageViewModel<ILibraryViewModel>, ILibraryViewModel
{
    private readonly IConnection _connection;
    private readonly ILibraryService _libraryService;

    public string EmptyLibrarySubtitleText { get; }

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

    public LibraryTreeDataGridAdapter Adapter { get; }

    public LibraryViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        LoadoutId loadoutId) : base(windowManager)
    {
        var ticker = Observable
            .Interval(period: TimeSpan.FromSeconds(10), timeProvider: ObservableSystem.DefaultTimeProvider)
            .Select(_ => DateTime.Now)
            .Publish(initialValue: DateTime.Now);

        Adapter = new LibraryTreeDataGridAdapter(serviceProvider, ticker);

        _advancedInstaller = serviceProvider.GetRequiredKeyedService<ILibraryItemInstaller>("AdvancedManualInstaller");

        TabTitle = "Library (new)";
        TabIcon = IconValues.ModLibrary;

        _libraryService = serviceProvider.GetRequiredService<ILibraryService>();
        _connection = serviceProvider.GetRequiredService<IConnection>();

        ticker.Connect();

        _loadout = Loadout.Load(_connection.Db, loadoutId.Value);
        var game = _loadout.InstallationInstance.Game;
        var gameDomain = game.Domain;

        EmptyLibrarySubtitleText = string.Format(Language.FileOriginsPageViewModel_EmptyLibrarySubtitleText, game.Name);

        SwitchViewCommand = new ReactiveCommand<Unit>(_ =>
        {
            Adapter.ViewHierarchical.Value = !Adapter.ViewHierarchical.Value;
        });

        var hasSelection = Adapter.SelectedModels
            .ObserveCountChanged()
            .Select(count => count > 0);

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
        var gameUri = new Uri($"https://www.nexusmods.com/{gameDomain}");

        OpenNexusModsCommand = new ReactiveCommand<Unit>(
            executeAsync: async (_, cancellationToken) => await osInterop.OpenUrl(gameUri, cancellationToken: cancellationToken),
            awaitOperation: AwaitOperation.Parallel,
            configureAwait: false
        );

        this.WhenActivated(disposables =>
        {
            Adapter.Activate();
            Disposable.Create(Adapter, static adapter => adapter.Deactivate()).AddTo(disposables);

            Disposable.Create(this, static vm => vm.StorageProvider = null).AddTo(disposables);

            Disposable.Create(this, static vm =>
            {
                foreach (var kv in vm.InstallCommandDisposables)
                {
                    var (_, disposable) = kv;
                    disposable.Dispose();
                }

                vm.InstallCommandDisposables.Clear();
            }).AddTo(disposables);

            Adapter.ModelActivationSubject
                .Subscribe(this, static (tuple, vm) =>
                {
                    var (model, isActivated) = tuple;

                    if (isActivated)
                    {
                        var disposable = model.InstallCommand.Subscribe(vm, static (libraryItemId, vm) => vm.InstallLibraryItemSubject.OnNext(libraryItemId));
                        var didAdd = vm.InstallCommandDisposables.TryAdd(model, disposable);
                        Debug.Assert(didAdd, "subscription for the model shouldn't exist yet");
                    }
                    else
                    {
                        var didRemove = vm.InstallCommandDisposables.Remove(model, out var disposable);
                        Debug.Assert(didRemove, "subscription for the model should exist");
                        disposable?.Dispose();
                    }
                })
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
        var items = Adapter.SelectedModels
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
}

public class LibraryTreeDataGridAdapter : TreeDataGridAdapter<LibraryItemModel>
{
    private readonly ILibraryDataProvider[] _libraryDataProviders;
    private readonly ConnectableObservable<DateTime> _ticker;

    public LibraryTreeDataGridAdapter(IServiceProvider serviceProvider, ConnectableObservable<DateTime> ticker)
    {
        _libraryDataProviders = serviceProvider.GetServices<ILibraryDataProvider>().ToArray();
        _ticker = ticker;
    }

    protected override void BeforeModelActivationHook(LibraryItemModel model)
    {
        model.Ticker = _ticker;
    }

    protected override void BeforeModelDeactivationHook(LibraryItemModel model)
    {
        model.Ticker = null;
    }

    protected override IObservable<IChangeSet<LibraryItemModel>> GetRootsObservable(bool viewHierarchical)
    {
        var observables = viewHierarchical
            ? _libraryDataProviders.Select(provider => provider.ObserveNestedLibraryItems())
            : _libraryDataProviders.Select(provider => provider.ObserveFlatLibraryItems());

        return observables.MergeChangeSets();
    }

    protected override IColumn<LibraryItemModel>[] CreateColumns(bool viewHierarchical)
    {
        var nameColumn = LibraryItemModel.CreateNameColumn();

        return
        [
            viewHierarchical ? LibraryItemModel.CreateExpanderColumn(nameColumn) : nameColumn,
            LibraryItemModel.CreateVersionColumn(),
            LibraryItemModel.CreateSizeColumn(),
            LibraryItemModel.CreateAddedAtColumn(),
            LibraryItemModel.CreateInstalledAtColumn(),
            LibraryItemModel.CreateInstallColumn(),
        ];
    }
}
