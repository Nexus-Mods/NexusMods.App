using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.CrossPlatform.Process;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Paths;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RocksDbSharp;

namespace NexusMods.App.UI.Pages.ModLibrary;

public class FileOriginsPageViewModel : APageViewModel<IFileOriginsPageViewModel>, IFileOriginsPageViewModel
{
    private readonly IConnection _conn;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<FileOriginsPageViewModel> _logger;
    private readonly IServiceProvider _provider;
    private readonly IFileOriginRegistry _fileOriginRegistry;
    private readonly IOSInterop _osInterop;
    private readonly IRepository<DownloadAnalysis.Model> _dlAnalysisRepo;
    private readonly IArchiveInstaller _archiveInstaller;

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
        }
    }

    public ReadOnlyObservableCollection<IFileOriginEntryViewModel> SelectedModsCollection => _selectedModsCollection;

    ReactiveCommand<Unit, Unit> IFileOriginsPageViewModel.AddMod => ReactiveCommand.CreateFromTask(AddMod);
    ReactiveCommand<Unit, Unit> IFileOriginsPageViewModel.AddModAdvanced => ReactiveCommand.CreateFromTask(AddModAdvanced);
    ReactiveCommand<Unit, Unit> IFileOriginsPageViewModel.OpenNexusModPage => ReactiveCommand.CreateFromTask(OpenNexusModPage);

    private IObservable<IChangeSet<IFileOriginEntryViewModel, IFileOriginEntryViewModel>> _selectedMods = null!; // set from View
    private ReadOnlyObservableCollection<IFileOriginEntryViewModel> _selectedModsCollection = null!; // set from View

    public LoadoutId LoadoutId { get; private set; }
    private readonly GameDomain _gameDomain;
    
    public FileOriginsPageViewModel(
        LoadoutId loadoutId,
        IServiceProvider serviceProvider) : base(serviceProvider.GetRequiredService<IWindowManager>())
    {
        _conn = serviceProvider.GetRequiredService<IConnection>();
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _logger = serviceProvider.GetRequiredService<ILogger<FileOriginsPageViewModel>>();
        _provider = serviceProvider;
        _fileOriginRegistry = serviceProvider.GetRequiredService<IFileOriginRegistry>();
        _osInterop = serviceProvider.GetRequiredService<IOSInterop>();
        _archiveInstaller = serviceProvider.GetRequiredService<IArchiveInstaller>();
        _dlAnalysisRepo = serviceProvider.GetRequiredService<IRepository<DownloadAnalysis.Model>>();

        TabTitle = Language.FileOriginsPageTitle;
        TabIcon = IconValues.ModLibrary;

        LoadoutId = loadoutId;

        var loadout = _conn.Db.Get<Loadout.Model>(LoadoutId.Value);
        var game = loadout.Installation.Game;
        _gameDomain = loadout.Installation.Game.Domain;

        _fileOrigins = new ReadOnlyObservableCollection<IFileOriginEntryViewModel>([]);
        this.WhenActivated(d =>
        {
            var workspaceController = GetWorkspaceController();
            var entriesObservable = _dlAnalysisRepo.Observable
                .ToObservableChangeSet()
                .Filter(model => FilterDownloadAnalysisModel(model, game.Domain))
                .OnUI()
                .Transform(fileOrigin => (IFileOriginEntryViewModel)
                    new FileOriginEntryViewModel(
                        _conn,
                        _archiveInstaller,
                        LoadoutId,
                        fileOrigin,
                        workspaceController
                    )
                )
                .Bind(out _fileOrigins);
        
            entriesObservable.SubscribeWithErrorLogging().DisposeWith(d);
        });
    }

    public static bool FilterDownloadAnalysisModel(DownloadAnalysis.Model model, GameDomain currentGameDomain)
    {
        if (!model.TryGet(DownloaderState.GameDomain, out var domain)) return false;
        if (domain != currentGameDomain) return false;
        if (model.Contains(StreamBasedFileOriginMetadata.StreamBasedOrigin)) return false;
        return true;
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
            await _fileOriginRegistry.RegisterDownload(file,
                (tx, id) =>
                {
                    tx.Add(id, DownloaderState.GameDomain, _gameDomain);
                    tx.Add(id, FilePathMetadata.OriginalName, file.FileName);
                }, file.FileName);
        });

        return Task.CompletedTask;
    }

    public async Task OpenNexusModPage()
    {
        var url = $"https://www.nexusmods.com/{_gameDomain.Value}";
        await _osInterop.OpenUrl(new Uri(url), true);
    }

    public async Task AddMod() => await AddMod(null);

    public async Task AddModAdvanced() => await AddMod(_provider.GetKeyedService<IModInstaller>("AdvancedManualInstaller"));

    private async Task AddMod(IModInstaller? installer)
    {
        foreach (var mod in SelectedModsCollection)
        {
            await mod.AddToLoadoutCommand.Execute(installer);
        }
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
