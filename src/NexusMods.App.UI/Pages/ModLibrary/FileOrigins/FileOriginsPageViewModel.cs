using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
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
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Tasks.State;
using NexusMods.Paths;
using ReactiveUI;
using RocksDbSharp;

namespace NexusMods.App.UI.Pages.ModLibrary;

public class FileOriginsPageViewModel : APageViewModel<IFileOriginsPageViewModel>, IFileOriginsPageViewModel
{
    private readonly IConnection _conn;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<FileOriginsPageViewModel> _logger;
    private readonly IServiceProvider _provider;
    private readonly IFileOriginRegistry _fileOriginRegistry;
    private readonly IRepository<DownloadAnalysis.Model> _dlAnalysisRepo;
    private readonly IArchiveInstaller _archiveInstaller;

    public ReadOnlyObservableCollection<IFileOriginEntryViewModel> FileOrigins => _fileOrigins;
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
        IWindowManager windowManager) : base(windowManager)
    {
        _conn = conn;
        _fileSystem = fileSystem;
        _logger = logger;
        _provider = provider;
        _fileOriginRegistry = fileOriginRegistry;
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
                .Transform(fileOrigin => (IFileOriginEntryViewModel)new FileOriginEntryViewModel(
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

    public Task AddMod(string path) => AddMod(path, installer: null);

    public Task AddModAdvanced(string path)
    {
        var installer = _provider.GetKeyedService<IModInstaller>("AdvancedInstaller");
        return AddMod(path, installer);
    }

    private Task AddMod(string path, IModInstaller? installer)
    {
        var file = _fileSystem.FromUnsanitizedFullPath(path);
        if (!_fileSystem.FileExists(file))
        {
            _logger.LogError("File {File} does not exist, not installing mod", file);
            return Task.CompletedTask;
        }

        var _ = Task.Run(async () =>
        {
            var downloadId = await _fileOriginRegistry.RegisterDownload(file,
                (tx, id) =>
                {
                    tx.Add(id, DownloaderState.GameDomain, _gameDomain);
                    tx.Add(id, FilePathMetadata.OriginalName, file.FileName);
                });
            await _archiveInstaller.AddMods(LoadoutId, downloadId, file.FileName, token: CancellationToken.None, installer: installer);
        });

        return Task.CompletedTask;
    }
}
