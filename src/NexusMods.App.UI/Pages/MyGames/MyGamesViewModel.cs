using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk.Icons;
using NexusMods.MnemonicDB.Abstractions;
using OneOf;
using OneOf.Types;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData.Aggregation;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Sdk.Settings;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Controls.MiniGameWidget.ComingSoon;
using NexusMods.App.UI.Controls.MiniGameWidget.Standard;
using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Settings;
using NexusMods.Collections;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.Jobs;
using NexusMods.Telemetry;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Dialog;
using NexusMods.UI.Sdk.Dialog.Enums;

namespace NexusMods.App.UI.Pages.MyGames;

[UsedImplicitly]
public class MyGamesViewModel : APageViewModel<IMyGamesViewModel>, IMyGamesViewModel
{
    private const string TrelloPublicRoadmapUrl = "https://trello.com/b/gPzMuIr3/nexus-mods-app-roadmap";

    private readonly ILibraryService _libraryService;
    private readonly CollectionDownloader _collectionDownloader;
    private readonly IWindowManager _windowManager;
    private readonly IJobMonitor _jobMonitor;
    private readonly IOverlayController _overlayController;
    private readonly IConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISynchronizerService _syncService;
    private readonly ILoadoutManager _loadoutManager;

    private ReadOnlyObservableCollection<IViewModelInterface> _supportedGames = new([]);
    private ReadOnlyObservableCollection<IGameWidgetViewModel> _installedGames = new([]);

    public ReactiveCommand<Unit, Unit> OpenRoadmapCommand { get; }
    public ReadOnlyObservableCollection<IGameWidgetViewModel> InstalledGames => _installedGames;
    public ReadOnlyObservableCollection<IViewModelInterface> SupportedGames => _supportedGames;

    public MyGamesViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        IConnection conn,
        ILogger<MyGamesViewModel> logger,
        IOverlayController overlayController,
        IOSInterop osInterop,
        ISynchronizerService syncService,
        IGameRegistry gameRegistry) : base(windowManager)
    {
        var settingsManager = serviceProvider.GetRequiredService<ISettingsManager>();
        var experimentalSettings = settingsManager.Get<ExperimentalSettings>();

        var libraryDataProviders = serviceProvider.GetServices<ILibraryDataProvider>().ToArray();

        _collectionDownloader = serviceProvider.GetRequiredService<CollectionDownloader>();
        _libraryService = serviceProvider.GetRequiredService<ILibraryService>();
        _jobMonitor = serviceProvider.GetRequiredService<IJobMonitor>();
        _overlayController = overlayController;
        _connection = conn;
        _loadoutManager = serviceProvider.GetRequiredService<ILoadoutManager>();

        TabTitle = Language.MyGames;
        TabIcon = IconValues.GamepadOutline;

        _serviceProvider = serviceProvider;
        _syncService = syncService;
        _windowManager = windowManager;

        OpenRoadmapCommand = ReactiveCommand.Create(() =>
        {
            var uri = new Uri(TrelloPublicRoadmapUrl);
            osInterop.OpenUri(uri);
        });

        this.WhenActivated(d =>
            {
                gameRegistry.InstalledGames
                    .Where(game =>
                    {
                        if (experimentalSettings.EnableAllGames) return true;
                        return experimentalSettings.SupportedGames.Contains(game.Game.NexusModsGameId.Value);
                    })
                    .ToReadOnlyObservableCollection()
                    .ToObservableChangeSet()
                    .Transform(installation =>
                        {
                            var vm = _serviceProvider.GetRequiredService<IGameWidgetViewModel>();
                            vm.Installation = installation;

                            vm.AddGameCommand = ReactiveCommand.CreateFromTask(async () => await AddGameHandler(installation, vm));

                            vm.RemoveAllLoadoutsCommand = ReactiveCommand.CreateFromTask(async () =>
                            {
                                if (GetJobRunningForGameInstallation(installation).IsT2) return;

                                var filesToDelete = libraryDataProviders.SelectMany(dataProvider => dataProvider.GetAllFiles(gameId: installation.Game.NexusModsGameId.Value)).ToArray();
                                var totalSize = filesToDelete.Sum(static Size (file) => file.Size);

                                var collections = CollectionDownloader.GetCollections(conn.Db, installation.Game.NexusModsGameId.Value);

                                var overlay = new RemoveGameOverlayViewModel
                                {
                                    GameName = installation.Game.DisplayName,
                                    NumDownloads = filesToDelete.Length,
                                    SumDownloadsSize = totalSize,
                                    NumCollections = collections.Length,
                                };

                                var result = await overlayController.EnqueueAndWait(overlay);
                                if (!result.ShouldRemoveGame) return;

                                vm.State = GameWidgetState.RemovingGame;
                                await Task.Run(async () => await RemoveGame(installation, shouldDeleteDownloads: result.ShouldDeleteDownloads, filesToDelete, collections));
                                vm.State = GameWidgetState.DetectedGame;

                                Tracking.AddEvent(Events.Game.RemoveGame, new EventMetadata(name: $"{installation.Game.DisplayName} - {installation.Store}"));
                            });

                            vm.ViewGameCommand = ReactiveCommand.Create(() =>
                            {
                                NavigateToLoadoutLibrary(conn, installation);
                                Tracking.AddEvent(Events.Game.ViewGame, new EventMetadata(name: $"{installation.Game.DisplayName} - {installation.Store}"));
                            });

                            vm.IsManagedObservable = Loadout.ObserveAll(conn)
                                .Filter(l => l.IsVisible() && l.InstallationInstance.GameMetadataId == installation.GameMetadataId)
                                .Count()
                                .Select(c => c > 0);

                            var job = GetJobRunningForGameInstallation(installation);

                            // fixes when the page loads and a job is still running
                            vm.State = job.Value switch
                            {
                                CreateLoadoutJob _ => GameWidgetState.AddingGame,
                                UnmanageGameJob _ => GameWidgetState.RemovingGame,
                                _ => GameWidgetState.DetectedGame,
                            };

                            return vm;
                        }
                    )
                    .OnUI()
                    .Bind(out _installedGames)
                    .SubscribeWithErrorLogging()
                    .DisposeWith(d);

                // NOTE(insomnious): The weird cast is so that we don't get a circular reference with Abstractions.Games when
                // referencing Abstractions.GameLocators directly 
                var supportedGamesAsIGame = gameRegistry
                    .SupportedGames
                    .Where(game =>
                    {
                        if (experimentalSettings.EnableAllGames) return true;
                        return experimentalSettings.SupportedGames.Contains(game.NexusModsGameId.Value);
                    })
                    .Cast<IGame>()
                    .Where(game => _installedGames.All(install => install.Installation.GetGame().NexusModsGameId != game.NexusModsGameId)); // Exclude found games
                
                
                var miniGameWidgetViewModels = supportedGamesAsIGame
                    .Select(game =>
                        {
                            var vm = _serviceProvider.GetRequiredService<IMiniGameWidgetViewModel>();
                            vm.Game = game;
                            vm.Name = game.DisplayName;
                            // is this supported game installed?
                            vm.IsFound = _installedGames.Any(install => install.Installation.GetGame().NexusModsGameId == game.NexusModsGameId);
                            vm.GameInstallations = _installedGames
                                .Where(install => install.Installation.GetGame().NexusModsGameId == game.NexusModsGameId)
                                .Select(install => install.Installation)
                                .ToArray();
                            return vm;
                        }
                    )
                    .OrderByDescending(vm => vm.IsFound)
                    .ToList();

                var comingSoonMiniGameWidget = _serviceProvider.GetRequiredService<IComingSoonMiniGameWidgetViewModel>();

                // create a new ReadOnlyObservableCollection from miniGameWidgetViewModels and comingSoonMiniGameWidget
                _supportedGames = new ReadOnlyObservableCollection<IViewModelInterface>(
                    new ObservableCollection<IViewModelInterface>(miniGameWidgetViewModels) {
                    // Add the coming soon widget to the end of the list
                    comingSoonMiniGameWidget,
                });
            }
        );
    }

    private OneOf<None, CreateLoadoutJob, UnmanageGameJob> GetJobRunningForGameInstallation(GameInstallation installation)
    {
        foreach (var job in _jobMonitor.Jobs)
        {
            if (job.Status != JobStatus.Running) continue;

            if (job.Definition is CreateLoadoutJob createLoadoutJob && createLoadoutJob.Installation.Equals(installation)) return createLoadoutJob;
            if (job.Definition is UnmanageGameJob unmanageGameJob && unmanageGameJob.Installation.Equals(installation)) return unmanageGameJob;
        }

        return OneOf<None, CreateLoadoutJob, UnmanageGameJob>.FromT0(new None());
    }

    private async Task RemoveGame(GameInstallation installation, bool shouldDeleteDownloads, LibraryFile.ReadOnly[] filesToDelete, CollectionMetadata.ReadOnly[] collections)
    {
        await _syncService.UnManage(installation);

        if (!shouldDeleteDownloads) return;
        await _libraryService.RemoveLibraryItems(filesToDelete.Select(file => file.AsLibraryItem()));

        foreach (var collection in collections)
        {
            await _collectionDownloader.DeleteCollection(collection);
        }
    }
    
    private async Task AddGameHandler(GameInstallation installation, IGameWidgetViewModel vm)
    {
        if (GetJobRunningForGameInstallation(installation).IsT1) return;

        vm.State = GameWidgetState.AddingGame;
        var loadout = await Task.Run(async () => await ManageGame(installation));
        
        // Check if there are external changes
        var changeEntries = await GetExternalChangesItems(loadout);
        vm.State = GameWidgetState.ManagedGame;
        
        // Offer to clean them up
        if (changeEntries.Length > 0)
        {
            var (revert, doNothing, clean) = (ButtonDefinitionId.Cancel, ButtonDefinitionId.From("doNothing"), ButtonDefinitionId.Accept);
            var result = await ShowCleanGameFolderDialog(revert,
                doNothing,
                clean,
                changeEntries,
                installation
            );
            
            if (result == revert)
            {
                // Revert the loadout creation
                vm.State = GameWidgetState.RemovingGame;
                await Task.Run(async () => await _syncService.UnManage(installation, cleanGameFolder: false));
                vm.State = GameWidgetState.DetectedGame;
                
                Tracking.AddEvent(Events.Game.RevertManageOnDirty, new EventMetadata(name: $"{installation.Game.DisplayName} - {installation.Store}"));
                return;
            }
            if (result == clean)
            {
                vm.State = GameWidgetState.AddingGame;
                await CleanGameFolder(installation, loadout);
                vm.State = GameWidgetState.ManagedGame;
                
                Tracking.AddEvent(Events.Game.CleanGameOnManage, new EventMetadata(name: $"{installation.Game.DisplayName} - {installation.Store}"));
            }
            
            // do nothing, so keep the files
            Tracking.AddEvent(Events.Game.KeepDirtyOnManage, new EventMetadata(name: $"{installation.Game.DisplayName} - {installation.Store}"));
        }
        
        Tracking.AddEvent(Events.Game.AddGame, new EventMetadata(name: $"{installation.Game.DisplayName} - {installation.Store}"));
        NavigateToLoadoutLibrary(_connection, installation);
    }
    
    private ValueTask<LoadoutItemWithTargetPath.ReadOnly[]> GetExternalChangesItems(Loadout.ReadOnly loadout)
    {
        var db = _connection.Db;
        if (!LoadoutOverridesGroup.FindByOverridesFor(db, loadout.Id).TryGetFirst(out var overrideGroup))
            return ValueTask.FromResult<LoadoutItemWithTargetPath.ReadOnly[]>([]);

        return ValueTask.FromResult(overrideGroup.AsLoadoutItemGroup().Children.OfTypeLoadoutItemWithTargetPath().ToArray());
    }
    
    private async Task<ButtonDefinitionId> ShowCleanGameFolderDialog(
        ButtonDefinitionId revert,
        ButtonDefinitionId doNothing,
        ButtonDefinitionId clean,
        LoadoutItemWithTargetPath.ReadOnly[] changeEntries,
        GameInstallation installation)
    {
        var markdownVm = _serviceProvider.GetRequiredService<IMarkdownRendererViewModel>();
        markdownVm.Contents = $"""
            We found {changeEntries.Length} files in the game folder that aren’t part of a clean install. To avoid conflicts with mods, **we recommend starting with a clean folder**.
            #### Important
            If you keep existing files (not recommended):
            - The existing files will be placed in **External Changes**.
            - Files in External Changes **override any mods you install later**.
            - If you stop managing this game or uninstall the app, those **files will be permanently removed**.
            """;
        
        var dialog = DialogFactory.CreateStandardDialog(
            title: $"Your {installation.Game.DisplayName} folder isn't a clean install",
            new StandardDialogParameters()
            {
                Markdown = markdownVm,
            },
            buttonDefinitions:
            [
                new DialogButtonDefinition("Cancel", revert),
                new DialogButtonDefinition("Keep existing files", doNothing, ButtonAction.Reject),
                new DialogButtonDefinition("Clean folder", clean, ButtonAction.Accept, ButtonStyling.Primary),
            ]
        );
        
        return (await _windowManager.ShowDialog(dialog, DialogWindowType.Modal)).ButtonId;
    }
    
    private async Task CleanGameFolder(GameInstallation installation, Loadout.ReadOnly loadout)
    {
        var db = _connection.Db;
        var tx = _connection.BeginTransaction();
        var changeEntries = await GetExternalChangesItems(loadout.Rebase()); 
        
        // Remove items from External Changes mod
        foreach (var entry in changeEntries)
        {
            tx.Delete(entry.Id, recursive: false);
        }
        await tx.Commit();

        loadout = loadout.Rebase();
        var game = installation.GetGame();
        var syncer = game.Synchronizer;
        
        // Apply clean state to game folder
        await syncer.Synchronize(Loadout.Load(db, loadout));
    }

    private async Task<Loadout.ReadOnly> ManageGame(GameInstallation installation)
    {
        return await _loadoutManager.CreateLoadout(installation);
    }

    private Optional<LoadoutId> GetLoadout(IConnection conn, GameInstallation installation)
    {
        var db = conn.Db;

        var gameMetadata = GameInstallMetadata.Load(conn.Db, installation.GameMetadataId);
        if (gameMetadata.Contains(GameInstallMetadata.LastSyncedLoadout))
        {
            return gameMetadata.LastSyncedLoadout.LoadoutId;
        }

        // no applied loadout, return the first one
        var loadout = Loadout.All(db).FirstOrOptional(loadout =>
        loadout.IsVisible() && loadout.InstallationInstance.Equals(installation));

        return loadout.HasValue ? loadout.Value.LoadoutId : Optional<LoadoutId>.None;
    }
    
    private void NavigateToLoadoutLibrary(IConnection conn, GameInstallation installation)
    {
        var fistLoadout = GetLoadout(conn, installation);
        if (!fistLoadout.HasValue) return;
        var loadoutId = fistLoadout.Value;
        Dispatcher.UIThread.Invoke(() =>
            {
                var workspaceController = _windowManager.ActiveWorkspaceController;
                
                workspaceController.ChangeOrCreateWorkspaceByContext(
                    context => context.LoadoutId == loadoutId,
                    () => new PageData
                    {
                        FactoryId = LibraryPageFactory.StaticId,
                        Context = new LibraryPageContext()
                        {
                            LoadoutId = loadoutId,
                        },
                    },
                    () => new LoadoutContext
                    {
                        LoadoutId = loadoutId,
                    }
                );
            }
        );
    }
}
