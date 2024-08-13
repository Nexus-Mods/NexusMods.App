using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Platform.Storage;
using DynamicData;
using DynamicData.Alias;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.LoadoutGrid;
using NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.CrossPlatform.Process;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ModLibrary;

public class FileOriginsPageViewModel : APageViewModel<IFileOriginsPageViewModel>, IFileOriginsPageViewModel
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<FileOriginsPageViewModel> _logger;
    private readonly IServiceProvider _provider;
    private readonly IOSInterop _osInterop;

    public ReadOnlyObservableCollection<IFileOriginEntryViewModel> FileOrigins => _fileOrigins;
    private ReadOnlyObservableCollection<IFileOriginEntryViewModel> _fileOrigins;

    // Note(sewer). Adding a [Reactive] attribute breaks the setter, don't do it please.
    // For some reason the IL weaver used here discards the contents of the setter.
    // Which is strange because the common Fody PropertyChanged weaver doesn't suffer from this.
    // So this is down to most likely a bug in the custom implementation here.
    public IObservable<IChangeSet<IFileOriginEntryViewModel, IFileOriginEntryViewModel>> SelectedModsObservable
    {
        get => _selectedMods;
        set
        {
            // Called from view.
            _selectedMods = value;
            _selectedMods.Bind(out _selectedModsCollection).Subscribe();
            this.RaisePropertyChanged();
        }
    }

    public ReadOnlyObservableCollection<IFileOriginEntryViewModel> SelectedModsCollection => _selectedModsCollection;
    public ReactiveCommand<Unit, Unit> RemoveMod { get; }

    public string EmptyLibrarySubtitleText { get; }

    public ReactiveCommand<Unit, Unit> AddMod { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> AddModAdvanced { get; private set; } = null!;
    ReactiveCommand<Unit, Unit> IFileOriginsPageViewModel.OpenNexusModPage => ReactiveCommand.CreateFromTask(OpenNexusModPage);

    private IObservable<IChangeSet<IFileOriginEntryViewModel, IFileOriginEntryViewModel>> _selectedMods = null!; // set from View
    private ReadOnlyObservableCollection<IFileOriginEntryViewModel> _selectedModsCollection = new([]); // overwritten from View

    public LoadoutId LoadoutId { get; private set; }
    private readonly GameDomain _gameDomain;
    private readonly ILibraryService _libraryService;
    private readonly IConnection _conn;

    public FileOriginsPageViewModel(
        LoadoutId loadoutId,
        IServiceProvider serviceProvider) : base(serviceProvider.GetRequiredService<IWindowManager>())
    {
        _conn = serviceProvider.GetRequiredService<IConnection>();
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _logger = serviceProvider.GetRequiredService<ILogger<FileOriginsPageViewModel>>();
        _provider = serviceProvider;
        _osInterop = serviceProvider.GetRequiredService<IOSInterop>();
        _libraryService = serviceProvider.GetRequiredService<ILibraryService>();

        TabTitle = Language.FileOriginsPageTitle;
        TabIcon = IconValues.ModLibrary;

        LoadoutId = loadoutId;

        var loadout = Loadout.Load(_conn.Db, loadoutId);
        var game = loadout.InstallationInstance.Game;
        _gameDomain = loadout.InstallationInstance.Game.Domain;

        _fileOrigins = new ReadOnlyObservableCollection<IFileOriginEntryViewModel>([]);

        var hasSelection = new Subject<bool>();
        var advancedInstaller = _provider.GetKeyedService<ILibraryItemInstaller>("AdvancedManualInstaller");
        AddMod = ReactiveCommand.CreateFromTask(async cancellationToken => await DoAddModImpl(null, cancellationToken), hasSelection);
        AddModAdvanced = ReactiveCommand.CreateFromTask(async cancellationToken =>
        {
            await DoAddModImpl(advancedInstaller, cancellationToken);
        }, hasSelection);
        RemoveMod = ReactiveCommand.CreateFromTask(async token =>
        {
            await RemoveSelectedItems(token);
        }, hasSelection);

        EmptyLibrarySubtitleText = string.Format(Language.FileOriginsPageViewModel_EmptyLibrarySubtitleText, game.Name);
        
        this.WhenActivated(d =>
        {
            var workspaceController = GetWorkspaceController();
            
            // TODO: Move this to the entry if we ever adding scrolling to an entry.
            var viewModCommand = ReactiveCommand.Create<NavigationInformation>(info =>
                {
                    // Note(sewer): Design currently doesn't require we scroll to item,
                    //              (it's challenging) so just navigating to correct
                    //              view is enough.
                    var pageData = new PageData()
                    {
                        FactoryId = LoadoutGridPageFactory.StaticId,
                        Context = new LoadoutGridContext()
                        {
                            LoadoutId = loadoutId,
                        }
                    };

                    var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
                    workspaceController.OpenPage(workspaceController.ActiveWorkspaceId, pageData, behavior);
                }
            );

            LibraryUserFilters.ObserveFilteredLibraryItems(connection: _conn)
                .Select(item => item.ToLibraryFile())
                .Where(file => file.IsValid())
                .OnUI()
                .Transform(libraryFile => (IFileOriginEntryViewModel)
                    new FileOriginEntryViewModel(
                        _conn,
                        LoadoutId,
                        libraryFile,
                        viewModCommand,
                        ReactiveCommand.CreateFromTask(async () => await AddUsingInstallerToLoadout(libraryFile.AsLibraryItem(), null, default(CancellationToken))),
                        ReactiveCommand.CreateFromTask(async () => await AddUsingInstallerToLoadout(libraryFile.AsLibraryItem(), advancedInstaller, default(CancellationToken)))
                    )
                )
                .Bind(out _fileOrigins)
                .SubscribeWithErrorLogging().DisposeWith(d);
            
            // Note(sewer) This ensures inner is auto unsubscribed as new `SelectedModsObservable` items arrive.
            var serialDisposable = new SerialDisposable();
            serialDisposable.DisposeWith(d);
            this.WhenAnyValue(vm => vm.SelectedModsObservable)
                .Where(observable => observable != null!)
                .Select(observable =>
                {
                    return observable
                        .Select(_ => SelectedModsCollection.Count > 0)
                        .SubscribeWithErrorLogging(hasSel => hasSelection.OnNext(hasSel));
                })
                .SubscribeWithErrorLogging(disposable => serialDisposable.Disposable = disposable)
                .DisposeWith(d);
        });
    }
    private async Task RemoveSelectedItems(CancellationToken ct)
    {
        var itemsToRemove = SelectedModsCollection.ToList();
        await _libraryService.RemoveItems(itemsToRemove.Select(x => x.LibraryFile.AsLibraryItem()));
    }

    public async Task RegisterFromDisk(IStorageProvider storageProvider)
    {
        var files = await PickModFiles(storageProvider);
        foreach (var file in files)
            await RegisterFileFromDisk(file.Path.LocalPath);
    }

    private Task RegisterFileFromDisk(string path)
    {
        var file = _fileSystem.FromUnsanitizedFullPath(path);
        if (!_fileSystem.FileExists(file))
        {
            _logger.LogError("File {File} does not exist, not installing mod", file);
            return Task.CompletedTask;
        }

        _ = Task.Run(async () =>
        {
            await using var job = _libraryService.AddLocalFile(file);
            await job.StartAsync();
            await job.WaitToFinishAsync();
        });

        return Task.CompletedTask;
    }

    public async Task OpenNexusModPage()
    {
        var uri = NexusModsUrlBuilder.CreateGenericUri($"https://www.nexusmods.com/{_gameDomain.Value}");
        await _osInterop.OpenUrl(uri, true);
    }

    private async Task DoAddModImpl(ILibraryItemInstaller? installer, CancellationToken token)
    {
        foreach (var mod in SelectedModsCollection)
            await AddUsingInstallerToLoadout(mod.LibraryFile.AsLibraryItem(), installer, token);
    }

    private async Task AddUsingInstallerToLoadout(LibraryItem.ReadOnly libraryItem, ILibraryItemInstaller? installer, CancellationToken token)
    {
        var loadout = Loadout.Load(_conn.Db, LoadoutId);
        await using var job = _libraryService.InstallItem(libraryItem, loadout, installer);
        await job.StartAsync(token);
        await job.WaitToFinishAsync(token);
    }

    private async Task<IEnumerable<IStorageFile>> PickModFiles(IStorageProvider storageProvider)
    {
        var options =
            new FilePickerOpenOptions
            {
                Title = Language.LoadoutGridView_AddMod_FilePicker_Title,
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType(Language.LoadoutGridView_AddMod_FileType_Archive) {Patterns = new [] {"*.zip", "*.7z", "*.rar"}},
                }
            };

        return await storageProvider.OpenFilePickerAsync(options);
    }
}
