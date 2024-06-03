using System.Collections.ObjectModel;
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
    [Reactive] public IReadOnlyList<IFileOriginEntryViewModel> SelectedMods { get; set; } = Array.Empty<IFileOriginEntryViewModel>();
    private ReadOnlyObservableCollection<IFileOriginEntryViewModel> _fileOrigins = new([]);

    public LoadoutId LoadoutId { get; private set; }
    private GameDomain _gameDomain;


    public FileOriginsPageViewModel(
        LoadoutId loadoutId,
        IArchiveInstaller archiveInstaller,
        IRepository<DownloadAnalysis.Model> downloadAnalysisRepository,
        IConnection conn,
        IFileSystem fileSystem,
        ILogger<FileOriginsPageViewModel> logger,
        IServiceProvider provider,
        IFileOriginRegistry fileOriginRegistry,
        IOSInterop osInterop,
        IWindowManager windowManager) : base(windowManager)
    {
        _conn = conn;
        _fileSystem = fileSystem;
        _logger = logger;
        _provider = provider;
        _fileOriginRegistry = fileOriginRegistry;
        _osInterop = osInterop;
        _archiveInstaller = archiveInstaller;
        _dlAnalysisRepo = downloadAnalysisRepository;

        TabTitle = Language.FileOriginsPageTitle;
        TabIcon = IconValues.ModLibrary;

        LoadoutId = loadoutId;

        var loadout = _conn.Db.Get<Loadout.Model>(LoadoutId.Value);
        var game = loadout.Installation.Game;
        try
        {
            _gameDomain = loadout.Installation.Game.Domain;
        }
        catch (Exception)
        {
            _gameDomain = GameDomain.DefaultValue;
        }

        var entriesObservable = downloadAnalysisRepository.Observable
                .ToObservableChangeSet()
                .Filter(model => FilterDownloadAnalysisModel(model, game.Domain))
                .OnUI()
                .Transform(fileOrigin => (IFileOriginEntryViewModel)
                    new FileOriginEntryViewModel(
                        _conn,
                        _archiveInstaller,
                        LoadoutId,
                        fileOrigin
                    )
                )
                .Bind(out _fileOrigins);

        this.WhenActivated(d =>
        {
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

    [UsedImplicitly]
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

    public async Task AddModAdvanced() => await AddMod(_provider.GetKeyedService<IModInstaller>("AdvancedInstaller"));

    private async Task AddMod(IModInstaller? installer)
    {
        foreach (var mod in SelectedMods)
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
