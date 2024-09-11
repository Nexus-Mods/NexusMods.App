using System.Diagnostics;
using System.Reactive.Linq;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Platform.Storage;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Pages.Library;
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

namespace NexusMods.App.UI.Pages.LibraryPage;

public class LibraryViewModel : APageViewModel<ILibraryViewModel>, ILibraryViewModel
{
    private readonly IConnection _connection;
    private readonly ILibraryService _libraryService;

    public string EmptyLibrarySubtitleText { get; }

    public ReactiveCommand<Unit> SwitchViewCommand { get; }

    public ReactiveCommand<Unit> InstallSelectedItemsCommand { get; }

    public ReactiveCommand<Unit> InstallSelectedItemsWithAdvancedInstallerCommand { get; }

    public ReactiveCommand<Unit> RemoveSelectedItemsCommand { get; }

    public ReactiveCommand<Unit> OpenFilePickerCommand { get; }

    public ReactiveCommand<Unit> OpenNexusModsCommand { get; }

    [Reactive] public IStorageProvider? StorageProvider { get; set; }

    private readonly IServiceProvider _serviceProvider;
    private readonly ILibraryItemInstaller _advancedInstaller;
    private readonly Loadout.ReadOnly _loadout;

    public LibraryTreeDataGridAdapter Adapter { get; }

    private BehaviorSubject<Optional<LoadoutId>> LoadoutSubject { get; } = new(Optional<LoadoutId>.None);

    public LibraryViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        LoadoutId loadoutId) : base(windowManager)
    {
        _serviceProvider = serviceProvider;
        _libraryService = serviceProvider.GetRequiredService<ILibraryService>();
        _connection = serviceProvider.GetRequiredService<IConnection>();

        var ticker = R3.Observable
            .Interval(period: TimeSpan.FromSeconds(30), timeProvider: ObservableSystem.DefaultTimeProvider)
            .ObserveOnUIThreadDispatcher()
            .Select(_ => DateTime.Now)
            .Publish(initialValue: DateTime.Now);

        var loadoutObservable = LoadoutSubject
            .Where(static id => id.HasValue)
            .Select(static id => id.Value)
            .AsSystemObservable()
            .Replay(bufferSize: 1);

        var gameObservable = loadoutObservable
            .Select(id => Loadout.Load(_connection.Db, id).InstallationInstance.Game)
            .Replay(bufferSize: 1);

        var libraryFilter = new LibraryFilter(
            loadoutObservable: loadoutObservable,
            gameObservable: gameObservable
        );

        Adapter = new LibraryTreeDataGridAdapter(serviceProvider, ticker, libraryFilter);
        LoadoutSubject.OnNext(loadoutId);

        _advancedInstaller = serviceProvider.GetRequiredKeyedService<ILibraryItemInstaller>("AdvancedManualInstaller");

        TabTitle = Language.LibraryPageTitle;
        TabIcon = IconValues.ModLibrary;

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

        RemoveSelectedItemsCommand = hasSelection.ToReactiveCommand<Unit>(
            executeAsync: (_, cancellationToken) => RemoveSelectedItems(cancellationToken),
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
        var gameUri = NexusModsUrlBuilder.CreateGenericUri($"https://www.nexusmods.com/{gameDomain}");

        OpenNexusModsCommand = new ReactiveCommand<Unit>(
            executeAsync: async (_, cancellationToken) => await osInterop.OpenUrl(gameUri, cancellationToken: cancellationToken),
            awaitOperation: AwaitOperation.Parallel,
            configureAwait: false
        );

        this.WhenActivated(disposables =>
        {
            disposables.Add(loadoutObservable.Connect());
            disposables.Add(gameObservable.Connect());

            Disposable.Create(this, static vm => vm.StorageProvider = null).AddTo(disposables);

            Adapter.Activate();
            Disposable.Create(Adapter, static adapter => adapter.Deactivate()).AddTo(disposables);

            Adapter.MessageSubject.SubscribeAwait(
                onNextAsync: async (message, cancellationToken) =>
                {
                    foreach (var id in message.Ids)
                    {
                        var libraryItem = LibraryItem.Load(_connection.Db, id);
                        if (!libraryItem.IsValid()) continue;
                        await InstallLibraryItem(libraryItem, _loadout, cancellationToken);
                    }
                },
                awaitOperation: AwaitOperation.Parallel,
                configureAwait: false
            ).AddTo(disposables);
        });
    }

    private async ValueTask InstallItems(LibraryItemId[] ids, bool useAdvancedInstaller, CancellationToken cancellationToken)
    {
        var db = _connection.Db;
        var items = ids
            .Select(id => LibraryItem.Load(db, id))
            .Where(x => x.IsValid())
            .ToArray();

        await Parallel.ForAsync(
            fromInclusive: 0,
            toExclusive: items.Length,
            body: (i, innerCancellationToken) => InstallLibraryItem(items[i], _loadout, innerCancellationToken, useAdvancedInstaller),
            cancellationToken: cancellationToken
        );
    }

    private LibraryItemId[] GetSelectedIds()
    {
        return Adapter.SelectedModels
            .SelectMany(model => model.GetLoadoutItemIds())
            .Distinct()
            .ToArray();
    }

    private ValueTask InstallSelectedItems(bool useAdvancedInstaller, CancellationToken cancellationToken)
    {
        return InstallItems(GetSelectedIds(), useAdvancedInstaller, cancellationToken);
    }

    private async ValueTask InstallLibraryItem(
        LibraryItem.ReadOnly libraryItem,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken,
        bool useAdvancedInstaller = false)
    {
        await _libraryService.InstallItem(libraryItem, loadout, installer: useAdvancedInstaller ? _advancedInstaller : null);
    }

    private async ValueTask RemoveSelectedItems(CancellationToken cancellationToken)
    {
        var db = _connection.Db;
        var toRemove = GetSelectedIds().Select(id => LibraryItem.Load(db, id)).ToArray();
        await LibraryItemRemover.RemoveAsync(_connection, _serviceProvider.GetRequiredService<IOverlayController>(), _libraryService, toRemove, cancellationToken);
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
                var path = paths[i];
                await _libraryService.AddLocalFile(path);
            },
            cancellationToken: cancellationToken
        );
    }
}

public readonly record struct InstallMessage(IReadOnlyCollection<LibraryItemId> Ids);

public class LibraryTreeDataGridAdapter : TreeDataGridAdapter<LibraryItemModel, EntityId>,
    ITreeDataGirdMessageAdapter<InstallMessage>
{
    private readonly ILibraryDataProvider[] _libraryDataProviders;
    private readonly ConnectableObservable<DateTime> _ticker;
    private readonly LibraryFilter _libraryFilter;

    public Subject<InstallMessage> MessageSubject { get; } = new();
    private readonly Dictionary<LibraryItemModel, IDisposable> _commandDisposables = new();

    private readonly IDisposable _activationDisposable;
    public LibraryTreeDataGridAdapter(
        IServiceProvider serviceProvider,
        ConnectableObservable<DateTime> ticker,
        LibraryFilter libraryFilter)
    {
        _libraryDataProviders = serviceProvider.GetServices<ILibraryDataProvider>().ToArray();
        _ticker = ticker;
        _libraryFilter = libraryFilter;

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

    protected override void BeforeModelActivationHook(LibraryItemModel model)
    {
        model.Ticker = _ticker;

        var disposable = model.InstallCommand.Subscribe(MessageSubject, static (ids, subject) =>
        {
            subject.OnNext(new InstallMessage(ids));
        });

        var didAdd = _commandDisposables.TryAdd(model, disposable);
        Debug.Assert(didAdd, "subscription for the model shouldn't exist yet");

        base.BeforeModelActivationHook(model);
    }

    protected override void BeforeModelDeactivationHook(LibraryItemModel model)
    {
        model.Ticker = null;

        var didRemove = _commandDisposables.Remove(model, out var disposable);
        Debug.Assert(didRemove, "subscription for the model should exist");
        disposable?.Dispose();

        base.BeforeModelDeactivationHook(model);
    }

    protected override IObservable<IChangeSet<LibraryItemModel, EntityId>> GetRootsObservable(bool viewHierarchical)
    {
        var observables = viewHierarchical
            ? _libraryDataProviders.Select(provider => provider.ObserveNestedLibraryItems(_libraryFilter))
            : _libraryDataProviders.Select(provider => provider.ObserveFlatLibraryItems(_libraryFilter));

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
