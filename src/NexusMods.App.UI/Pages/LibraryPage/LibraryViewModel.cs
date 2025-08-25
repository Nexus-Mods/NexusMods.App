using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Platform.Storage;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Standard;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Pages.Library;
using NexusMods.App.UI.Pages.LibraryPage.Collections;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Collections;
using NexusMods.CrossPlatform.Process;
using NexusMods.UI.Sdk.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.UpdateFilters;
using NexusMods.Paths;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Dialog;
using NexusMods.UI.Sdk.Dialog.Enums;
using ObservableCollections;
using OneOf;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LibraryPage;

public class LibraryViewModel : APageViewModel<ILibraryViewModel>, ILibraryViewModel
{
    private readonly IConnection _connection;
    private readonly ILibraryService _libraryService;

    public string EmptyLibrarySubtitleText { get; }

    public ReactiveCommand<Unit> UpdateAllCommand { get; }
    public ReactiveCommand<Unit> RefreshUpdatesCommand { get; }
    public ReactiveCommand<Unit> InstallSelectedItemsCommand { get; }

    public ReactiveCommand<Unit> InstallSelectedItemsWithAdvancedInstallerCommand { get; }

    public ReactiveCommand<Unit> UpdateSelectedItemsCommand { get; }
    
    public ReactiveCommand<Unit> UpdateAndKeepOldSelectedItemsCommand { get; }

    public ReactiveCommand<Unit> RemoveSelectedItemsCommand { get; }
    
    public ReactiveCommand<Unit> DeselectItemsCommand { get; }

    public ReactiveCommand<Unit> OpenFilePickerCommand { get; }

    public ReactiveCommand<Unit> OpenNexusModsCommand { get; }
    public ReactiveCommand<Unit> OpenNexusModsCollectionsCommand { get; }

    [Reactive] public int SelectionCount { get; private set; }
    
    [Reactive] public int UpdatableSelectionCount { get; private set; }
    
    [Reactive] public bool HasAnyUpdatesAvailable { get; private set; }

    [Reactive] public bool IsUpdatingAll { get; private set; }

    [Reactive] public IStorageProvider? StorageProvider { get; set; }

    private readonly IServiceProvider _serviceProvider;
    private readonly ILibraryItemInstaller _advancedInstaller;
    private readonly IGameDomainToGameIdMappingCache _gameIdMappingCache;
    private readonly Loadout.ReadOnly _loadout;
    private readonly IModUpdateService _modUpdateService;
    private readonly ILoginManager _loginManager;
    private readonly NexusModsLibrary _nexusModsLibrary;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly IWindowNotificationService _notificationService;

    public LibraryTreeDataGridAdapter Adapter { get; }
    private ReadOnlyObservableCollection<ICollectionCardViewModel> _collections = new([]);
    public ReadOnlyObservableCollection<ICollectionCardViewModel> Collections => _collections;

    private ReadOnlyObservableCollection<InstallationTarget> _installationTargets = new([]);
    public ReadOnlyObservableCollection<InstallationTarget> InstallationTargets => _installationTargets;

    [Reactive] public InstallationTarget? SelectedInstallationTarget { get; set; }

    public LibraryViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        IGameDomainToGameIdMappingCache gameIdMappingCache,
        LoadoutId loadoutId) : base(windowManager)
    {
        _serviceProvider = serviceProvider;
        _gameIdMappingCache = gameIdMappingCache;
        _libraryService = serviceProvider.GetRequiredService<ILibraryService>();
        _nexusModsLibrary = serviceProvider.GetRequiredService<NexusModsLibrary>();
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _modUpdateService = serviceProvider.GetRequiredService<IModUpdateService>();
        _loginManager = serviceProvider.GetRequiredService<ILoginManager>();
        _temporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
        _notificationService = serviceProvider.GetRequiredService<IWindowNotificationService>();

        var collectionDownloader = new CollectionDownloader(serviceProvider);
        var tileImagePipeline = ImagePipelines.GetCollectionTileImagePipeline(serviceProvider);
        var userAvatarPipeline = ImagePipelines.GetUserAvatarPipeline(serviceProvider);

        var loadout = Loadout.Load(_connection.Db, loadoutId);
        var libraryFilter = new LibraryFilter(loadout, loadout.InstallationInstance.Game);

        Adapter = new LibraryTreeDataGridAdapter(serviceProvider, libraryFilter);

        _advancedInstaller = serviceProvider.GetRequiredKeyedService<ILibraryItemInstaller>("AdvancedManualInstaller_Direct");

        TabTitle = Language.LibraryPageTitle;
        TabIcon = IconValues.LibraryOutline;
        
        _loadout = Loadout.Load(_connection.Db, loadoutId.Value);
        var game = _loadout.InstallationInstance.Game;

        EmptyLibrarySubtitleText = string.Format(Language.FileOriginsPageViewModel_EmptyLibrarySubtitleText, game.Name);

        DeselectItemsCommand = new ReactiveCommand<Unit>(_ =>
        {
            Adapter.ClearSelection();
        });

        RefreshUpdatesCommand = new ReactiveCommand<Unit>(
            executeAsync: (_, token) => RefreshUpdates(token),
            awaitOperation: AwaitOperation.Switch
        );

        // Create an observable that determines when UpdateAll can execute
        var canUpdateAll = this.WhenAnyValue(vm => vm.IsUpdatingAll)
            .ToObservable()
            .Select(isUpdating => !isUpdating);

        UpdateAllCommand = canUpdateAll.ToReactiveCommand<Unit>(
            executeAsync: (_, cancellationToken) => UpdateAllItems(cancellationToken),
            awaitOperation: AwaitOperation.Sequential,
            initialCanExecute: true,
            configureAwait: false
        );

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

        var hasUpdatableSelection = Adapter.SelectedModels
            .ObserveCountChanged()
            .Select(_ => GetSelectedModelsWithUpdates().Any());

        var canUpdateSelected = R3.Observable.CombineLatest(
            hasUpdatableSelection,
            this.WhenAnyValue(vm => vm.IsUpdatingAll).ToObservable(),
            (hasUpdates, isUpdatingAll) => hasUpdates && !isUpdatingAll
        );

        UpdateSelectedItemsCommand = canUpdateSelected.ToReactiveCommand<Unit>(
            executeAsync: (_, cancellationToken) => UpdateSelectedItems(cancellationToken),
            awaitOperation: AwaitOperation.Parallel,
            initialCanExecute: false,
            configureAwait: false
        );

        var canUpdateAndKeepOldSelected = R3.Observable.CombineLatest(
            hasUpdatableSelection,
            this.WhenAnyValue(vm => vm.IsUpdatingAll).ToObservable(),
            (hasUpdates, isUpdatingAll) => hasUpdates && !isUpdatingAll
        );

        UpdateAndKeepOldSelectedItemsCommand = canUpdateAndKeepOldSelected.ToReactiveCommand<Unit>(
            executeAsync: (_, cancellationToken) => UpdateAndKeepOldSelectedItems(cancellationToken),
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
        OpenNexusModsCommand = new ReactiveCommand<Unit>(
            executeAsync: async (_, cancellationToken) =>
            {
                var gameDomain = _gameIdMappingCache[game.GameId];
                var gameUri = NexusModsUrlBuilder.GetGameUri(gameDomain);
                await osInterop.OpenUrl(gameUri, cancellationToken: cancellationToken);
            },
            awaitOperation: AwaitOperation.Parallel,
            configureAwait: false
        );
        OpenNexusModsCollectionsCommand = new ReactiveCommand<Unit>(
            executeAsync: async (_, cancellationToken) =>
            {
                var gameDomain = _gameIdMappingCache[game.GameId];
                var gameUri = NexusModsUrlBuilder.GetBrowseCollectionsUri(gameDomain);
                await osInterop.OpenUrl(gameUri, cancellationToken: cancellationToken);
            },
            awaitOperation: AwaitOperation.Parallel,
            configureAwait: false
        );

        this.WhenActivated(disposables =>
        {
            Disposable.Create(this, static vm => vm.StorageProvider = null).AddTo(disposables);
            Adapter.Activate().AddTo(disposables);

            Adapter.MessageSubject.SubscribeAwait(
                onNextAsync: async (message, cancellationToken) =>
                {
                    await message.Match(
                        async installMessage =>
                        {
                            foreach (var id in installMessage.Ids)
                            {
                                var libraryItem = LibraryItem.Load(_connection.Db, id);
                                if (!libraryItem.IsValid()) continue;
                                await InstallLibraryItem(libraryItem, _loadout, GetInstallationTarget(), cancellationToken);
                            }
                        },
                        async updateAndReplaceMessage => await HandleUpdateAndReplaceMessage(updateAndReplaceMessage, cancellationToken),
                        async updateAndKeepOldMessage => await HandleUpdateAndKeepOldMessage(updateAndKeepOldMessage, cancellationToken),
                        async viewChangelogMessage => await HandleViewChangelogMessage(viewChangelogMessage, cancellationToken),
                        async viewModPageMessage => await HandleViewModPageMessage(viewModPageMessage, cancellationToken),
                        async hideUpdatesMessage => await HandleHideUpdatesMessage(hideUpdatesMessage, cancellationToken),
                        async deleteItemMessage => await HandleDeleteItemMessage(deleteItemMessage, cancellationToken)
                    );
                },
                awaitOperation: AwaitOperation.Parallel,
                configureAwait: false
            ).AddTo(disposables);

            Loadout.MutableCollections(_connection, _loadout.LoadoutId)
                .Observe(r => r.GroupId)
                .Transform(r => new InstallationTarget(r.GroupId, r.Name))
                .SortBy(target => target.Id.Value.Value)
                .DistinctUntilChanged()
                .Bind(out _installationTargets)
                .Subscribe()
                .AddTo(disposables);

            CollectionRevisionMetadata.ObserveAll(_connection)
                .FilterImmutable(revision => revision.Collection.GameId == game.GameId)
                .OnUI()
                .Transform(ICollectionCardViewModel (revision) => new CollectionCardViewModel(
                    collectionDownloader: collectionDownloader,
                    tileImagePipeline: tileImagePipeline,
                    userAvatarPipeline: userAvatarPipeline,
                    windowManager: WindowManager,
                    workspaceId: WorkspaceId,
                    revision: revision,
                    targetLoadout: _loadout)
                )
                .Bind(out _collections)
                .Subscribe()
                .AddTo(disposables);
            
            // Update the selection count based on the selected models
            Adapter.SelectedModels
                .ObserveChanged()
                .ObserveOnUIThreadDispatcher()
                .Subscribe(_ => 
                {
                    SelectionCount = GetSelectedIds().Length;
                    UpdatableSelectionCount = GetSelectedModelsWithUpdates().Count();
                })
                .AddTo(disposables);

            // Subscribe to mod update service to automatically track "has any updates" state
            _modUpdateService.HasAnyUpdatesObservable()
                .OnUI()
                .Subscribe(hasUpdates => HasAnyUpdatesAvailable = hasUpdates)
                .AddTo(disposables);

            // Auto check updates on entering library.
            RefreshUpdatesCommand.Execute(Unit.Default);
        });
    }

    private async ValueTask HandleUpdateAndReplaceMessage(UpdateAndReplaceMessage updateAndReplaceMessage, CancellationToken cancellationToken)
    {
        var isPremium = _loginManager.IsPremium;
        if (!isPremium)
            // Note(sewer): Per design, in the future this will expand the mod rows.
            //              But, due to the TreeDataGrid bug, we can't do that today, yet.
            await UpdateAndReplaceForMultiModPagesFreeOnly(cancellationToken, [updateAndReplaceMessage.Updates]);
        else
            await UpdateAndReplaceForMultiModPagesPremiumOnly(cancellationToken, [updateAndReplaceMessage.Updates]);
    }

    private async ValueTask UpdateAndReplaceForMultiModPagesFreeOnly(CancellationToken cancellationToken, IEnumerable<ModUpdatesOnModPage> updatesOnPageCollection)
    {
        // Show the original dialog
        var dialog = DialogFactory.CreateStandardDialog(
            Language.Dialog_ReplaceNotSupported_Title,
            new StandardDialogParameters()
            {
                Text = Language.Dialog_ReplaceNotSupported_Text,
            },
            [DialogStandardButtons.Ok, DialogStandardButtons.Cancel]
        );
        
        var dialogResult = await WindowManager.ShowDialog(dialog, DialogWindowType.Modal);
        if (dialogResult.ButtonId != DialogStandardButtons.Ok.Id)
        {
            // User cancelled, don't proceed
            return;
        }
        
        await UpdateAndKeepOldFree(updatesOnPageCollection, cancellationToken);
    }

    private async ValueTask UpdateAndReplaceForMultiModPagesPremiumOnly(CancellationToken cancellationToken, IEnumerable<ModUpdatesOnModPage> updatesOnPageCollection)
    {
        // Collect all library items that will be updated by linking with file metadata.
        var newestToCurrentMapping = new Dictionary<NexusModsFileMetadata.ReadOnly, List<NexusModsFileMetadata.ReadOnly>>();
        foreach (var updatesOnPage in updatesOnPageCollection)
            updatesOnPage.NewestToCurrentFileMapping(newestToCurrentMapping);
        
        await UpdateAndReplaceForNewestToCurrentMappingPremiumOnly(cancellationToken, newestToCurrentMapping);
    }

    private async ValueTask UpdateAndReplaceForNewestToCurrentMappingPremiumOnly(CancellationToken cancellationToken, Dictionary<NexusModsFileMetadata.ReadOnly, List<NexusModsFileMetadata.ReadOnly>> newestToCurrentMapping)
    {
        var libraryItemsToUpdate = new List<LibraryItem.ReadOnly>();
        foreach (var (_, currentFiles) in newestToCurrentMapping)
        {
            foreach (var currentFile in currentFiles)
            {
                // Find existing library items for this file metadata
                var existingLibraryItems = NexusModsLibraryItem.FindByFileMetadata(_connection.Db, currentFile);
                foreach (var existingLibraryFile in existingLibraryItems)
                {
                    var currentLibraryItem = existingLibraryFile.AsLibraryItem();
                    libraryItemsToUpdate.Add(currentLibraryItem);
                }
            }
        }

        // Find all affected loadouts
        var collectionsAffected = _libraryService.CollectionsWithLibraryItems(libraryItemsToUpdate, excludeReadOnlyCollections: true);
        var affectedCollectionCount = collectionsAffected.Count;

        // If there is more than 1 non-readonly affected collection, show confirmation dialog
        if (affectedCollectionCount >= 2)
        {
            (ButtonDefinitionId updateButtonId, StandardDialogResult dialogResult) = await ShowInstalledInMultipleCollectionsDialog(collectionsAffected, affectedCollectionCount);
            if (dialogResult.ButtonId != updateButtonId)
            {
                // User cancelled, don't proceed with update
                return;
            }
        }

        var concurrencyLimit = _serviceProvider.GetRequiredService<ISettingsManager>().Get<DownloadSettings>().MaxParallelDownloads;
        var results = await ProcessDownloadsInParallel(newestToCurrentMapping, concurrencyLimit, cancellationToken);

        // Some (but not all) downloads failed, so show relevant dialog.
        if (results.DownloadErrors.Count > 0)
            await ShowSomeDownloadsFailedDialog(results.DownloadErrors, newestToCurrentMapping, cancellationToken);

        // Some installs failed, ask if they want to keep old files
        if (results.LibraryItemReplacementResults.Count(x => x.InstallResult != LibraryItemReplacementResult.Success) > 0)
            await ShowSomeInstallsFailedDialog(results.LibraryItemReplacementResults);

        // Remove all old library items where the install failed.
        var successfullyInstalledItems = results.LibraryItemReplacementResults.Where(x => x.InstallResult == LibraryItemReplacementResult.Success);
        await RemoveOldLibraryItemsIfNotInReadOnlyCollections(successfullyInstalledItems);
    }
    
    private async Task ShowSomeDownloadsFailedDialog(
        ConcurrentBag<DownloadError> downloadErrors,
        Dictionary<NexusModsFileMetadata.ReadOnly, List<NexusModsFileMetadata.ReadOnly>> newestToCurrentMapping,
        CancellationToken ct)
    {
        var modsWhichFailed = new StringBuilder();
        foreach (var err in downloadErrors)
            modsWhichFailed.AppendLine(err.File.Name);

        var cancelFailedDownloads = new DialogButtonDefinition(
            Text: Language.Library_Update_SomeDownloadsFailed_Option_Cancel,
            Id: ButtonDefinitionId.Cancel,
            ButtonAction: ButtonAction.Reject,
            ButtonStyling: ButtonStyling.None
        );
        var retryFailedDownloads = new DialogButtonDefinition(
            Text: Language.Library_Update_SomeDownloadsFailed_Option_Retry,
            Id: ButtonDefinitionId.Accept,
            ButtonAction: ButtonAction.Accept,
            ButtonStyling: ButtonStyling.Default
        );

        var allFailedDialog = DialogFactory.CreateStandardDialog(
            title: Language.Library_Update_SomeDownloadsFailed_Title,
            new StandardDialogParameters()
            {
                Text = string.Format(Language.Library_Update_SomeDownloadsFailed_Description, modsWhichFailed),
            },
            buttonDefinitions: [cancelFailedDownloads, retryFailedDownloads]
        );

        var result = await WindowManager.ShowDialog(allFailedDialog, DialogWindowType.Modal);
        if (result.ButtonId == retryFailedDownloads.Id)
        {
            var downloadErrorsTable = downloadErrors.Select(x => x.File).ToHashSet();
            var failedDownloads = newestToCurrentMapping.Where(x => downloadErrorsTable.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            await UpdateAndReplaceForNewestToCurrentMappingPremiumOnly(ct, failedDownloads);
        }
    }

    private async Task ShowSomeInstallsFailedDialog(ConcurrentBag<DownloadedProcessingResult> libraryItemReplacementResults)
    {
        var failedToInstallItems = new List<LibraryItem.ReadOnly>();
        foreach (var replaceResult in libraryItemReplacementResults)
        {
            if (replaceResult.InstallResult != LibraryItemReplacementResult.Success)
                failedToInstallItems.Add(replaceResult.NewItem);
        }

        var modNames = string.Join(Environment.NewLine, failedToInstallItems.Select(failed => failed.Name));
        var description = string.Format(Language.Library_Update_SomeInstallsFailed_Description, modNames);

        var deleteButtonId = ButtonDefinitionId.From("Delete");
        var keepButtonId = ButtonDefinitionId.From("Keep");

        var dialog = DialogFactory.CreateStandardDialog(
            title: Language.Library_Update_SomeInstallsFailed_Title,
            new StandardDialogParameters()
            {
                Text = description,
            },
            buttonDefinitions: [
                new DialogButtonDefinition(
                    Language.Library_Update_SomeInstallsFailed_DeleteFiles,
                    deleteButtonId,
                    ButtonAction.Accept,
                    ButtonStyling.Destructive
                ),
                new DialogButtonDefinition(
                    Language.Library_Update_SomeInstallsFailed_KeepFiles,
                    keepButtonId,
                    ButtonAction.Reject,
                    ButtonStyling.None
                )
            ]
        );

        var result = await WindowManager.ShowDialog(dialog, DialogWindowType.Modal);
        // User chose to delete the failed update files
        if (result.ButtonId == deleteButtonId)
            await _libraryService.RemoveLibraryItems(failedToInstallItems);
    }

    private async Task<(ButtonDefinitionId updateButtonId, StandardDialogResult dialogResult)> ShowInstalledInMultipleCollectionsDialog(IReadOnlyDictionary<CollectionGroup.ReadOnly, IReadOnlyList<(LibraryItem.ReadOnly libraryItem, LibraryLinkedLoadoutItem.ReadOnly linkedItem)>> collectionsAffected, int affectedCollectionCount)
    {
        var updateButtonId = ButtonDefinitionId.From("Update");
        var cancelButtonId = ButtonDefinitionId.From("Cancel");

        var dialogDesc = new StringBuilder()
            .AppendLine(Language.Library_Update_InstalledInMultipleCollections_Description1)
            .AppendLine();

        var collectionsGroupedByLoadout = collectionsAffected.Select(collection => collection.Key.AsLoadoutItemGroup().AsLoadoutItem())
            .GroupBy(item => item.Loadout.Name)
            .OrderBy(loadoutGroup => loadoutGroup.Key);

        foreach (var loadoutGroup in collectionsGroupedByLoadout)
        {
            dialogDesc.AppendLine($"{loadoutGroup.Key}:");
            foreach (var collection in loadoutGroup)
                dialogDesc.AppendLine($"{collection.Name}");
            
            dialogDesc.AppendLine();
        }

        dialogDesc.AppendLine(Language.Library_Update_InstalledInMultipleCollections_Description2);

        var confirmDialog = DialogFactory.CreateStandardDialog(
            title: Language.Library_Update_InstalledInMultipleCollections_Title,
            new StandardDialogParameters()
            {
                Text = dialogDesc.ToString(),
            },
            buttonDefinitions: [
                new DialogButtonDefinition(
                        Language.Library_Update_InstalledInMultipleCollections_Cancel,
                        cancelButtonId,
                        ButtonAction.Reject
                    ),
                    new DialogButtonDefinition(
                        string.Format(Language.Library_Update_InstalledInMultipleCollections_Ok, affectedCollectionCount),
                        updateButtonId,
                        ButtonAction.Accept,
                        ButtonStyling.Primary
                    )
            ]
        );

        var dialogResult = await WindowManager.ShowDialog(confirmDialog, DialogWindowType.Modal);
        return (updateButtonId, dialogResult);
    }

    /// <summary>
    /// This downloads a list of mods marked by <see cref="newestToCurrentMapping"/>.
    /// Every mod is installed separately into the library (as a separate transaction).
    /// </summary>
    /// <param name="newestToCurrentMapping">The items to process.</param>
    /// <param name="concurrencyLimit">Amount of items to process at the same time.</param>
    /// <param name="cancellationToken">The token that can be used to cancel this operation.</param>
    /// <returns>A struct containing all the processing results including successful downloads, errors, and mappings.</returns>
    private async Task<DownloadProcessingResults> ProcessDownloadsInParallel(
        Dictionary<NexusModsFileMetadata.ReadOnly, List<NexusModsFileMetadata.ReadOnly>> newestToCurrentMapping,
        int concurrencyLimit,
        CancellationToken cancellationToken)
    {
        var libraryItemReplacementResults = new ConcurrentBag<DownloadedProcessingResult>();
        var downloadErrors = new ConcurrentBag<DownloadError>();

        using var semaphore = new SemaphoreSlim(concurrencyLimit);
        var tasks = new List<Task>();

        // Helper function to process a single download with error handling
        async Task ProcessDownload(SemaphoreSlim sema, NexusModsFileMetadata.ReadOnly newestFile, List<NexusModsFileMetadata.ReadOnly> currentFiles)
        {
            var isTaken = false;
            try
            {
                isTaken = await sema.WaitAsync(Timeout.Infinite, cancellationToken);

                // Try a download.
                await using var tempPath = _temporaryFileManager.CreateFile();
                var job = await _nexusModsLibrary.CreateDownloadJob(tempPath, newestFile, cancellationToken: cancellationToken);
                var libraryFile = await _libraryService.AddDownload(job);
                var newLibraryItem = libraryFile.AsLibraryItem();

                // Map each current file to the new file
                foreach (var currentFile in currentFiles)
                {
                    // Find existing library items for this file metadata
                    var existingLibraryItems = NexusModsLibraryItem.FindByFileMetadata(_connection.Db, currentFile);
                    foreach (var existingLibraryFile in existingLibraryItems) // Should only be one.
                    {
                        var currentLibraryItem = existingLibraryFile.AsLibraryItem();

                        // Try to install the item right away
                        var options = new ReplaceLibraryItemOptions { IgnoreReadOnlyCollections = true };
                        var result = await _libraryService.ReplaceLinkedItemsInAllLoadouts(currentLibraryItem, newLibraryItem, options);
                        libraryItemReplacementResults.Add(new DownloadedProcessingResult()
                        {
                            OldItem = currentLibraryItem,
                            NewItem = newLibraryItem,
                            InstallResult = result,
                        });

                        if (result == LibraryItemReplacementResult.Success)
                            _notificationService.ShowToast(string.Format(Language.ToastNotification_Mod_updated____0_, newLibraryItem.Name), ToastNotificationVariant.Success);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ignored.
            }
            catch (Exception ex)
            {
                downloadErrors.Add(new DownloadError()
                {
                    File = newestFile,
                    Error = ex,
                });
            }
            finally
            {
                if (isTaken)
                    sema.Release();
            }
        }

        // Create tasks for all downloads
        foreach (var (newestFile, currentFiles) in newestToCurrentMapping)
        {
            tasks.Add(ProcessDownload(semaphore, newestFile, currentFiles));
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        return new DownloadProcessingResults(
            libraryItemReplacementResults,
            downloadErrors
        );
    }

    private async ValueTask HandleUpdateAndKeepOldMessage(UpdateAndKeepOldMessage updateAndKeepOldMessage, CancellationToken cancellationToken)
    {
        var updatesOnPage = updateAndKeepOldMessage.Updates;
        
        // Note(sewer)
        // If the user is a free user, they have to go to the website due to API restrictions.
        // For premium, we can start a download directly.
        var isPremium = _loginManager.IsPremium;
        if (!isPremium)
        {
            /*
               // Note(sewer): The commented code here is the correct behaviour
               // as intended per the phase one design. We temporarily need to alter
               // this behaviour due to the TreeDataGrid bug. When TreeDataGrid
               // is fixed, we can revert.

               // If there are multiple mods, we expand the row
               var treeNode = updateMessage.TreeNode;
               if (updatesOnPage.NumberOfModFilesToUpdate > 1)
               {
                   treeNode.IsExpanded = true; // 👈 TreeDataGrid bug. Doesn't handle PropertyChanged right.
               }
               else
               {
                   // Otherwise send them to the download page!!
                   var latestFile = updatesOnPage.NewestFile();
                   var modFileUrl = NexusModsUrlBuilder.CreateModFileDownloadUri(latestFile.Uid.FileId, latestFile.Uid.GameId);
                   var osInterop = _serviceProvider.GetRequiredService<IOSInterop>();
                   await osInterop.OpenUrl(modFileUrl, cancellationToken: cancellationToken);
               }
            */

            await UpdateAndKeepOldFree([updatesOnPage], cancellationToken);
        }
        else
        {
            await UpdateAndKeepOldPremium([updatesOnPage], cancellationToken);
        }
    }

    private async Task UpdateAndKeepOldFree(IEnumerable<ModUpdatesOnModPage> updatesOnPages, CancellationToken cancellationToken)
    {
        // Aggregate all unique files across all mod pages
        var newestToCurrentMapping = new Dictionary<NexusModsFileMetadata.ReadOnly, List<NexusModsFileMetadata.ReadOnly>>();
        foreach (var updatesOnPage in updatesOnPages)
            updatesOnPage.NewestToCurrentFileMapping(newestToCurrentMapping);
        
        // Open download page for every unique file
        var osInterop = _serviceProvider.GetRequiredService<IOSInterop>();
        foreach (var newestFile in newestToCurrentMapping.Keys)
        {
            var uri = NexusModsUrlBuilder.GetFileDownloadUri(newestFile.ModPage.GameDomain, newestFile.ModPage.Uid.ModId, newestFile.Uid.FileId, useNxmLink: true, campaign: NexusModsUrlBuilder.CampaignUpdates);
            await osInterop.OpenUrl(uri, cancellationToken: cancellationToken);
        }
    }

    private async Task UpdateAndKeepOldPremium(IEnumerable<ModUpdatesOnModPage> updatesOnPages, CancellationToken cancellationToken)
    {
        // Aggregate all unique files across all mod pages
        var newestToCurrentMapping = new Dictionary<NexusModsFileMetadata.ReadOnly, List<NexusModsFileMetadata.ReadOnly>>();
        foreach (var updatesOnPage in updatesOnPages)
            updatesOnPage.NewestToCurrentFileMapping(newestToCurrentMapping);
        
        // Note(sewer): There's usually just 1 file in like 99% of the cases here
        //              so no need to optimize around file reuse and TemporaryFileManager.
        foreach (var newestFile in newestToCurrentMapping.Keys)
        {
            await using var tempPath = _temporaryFileManager.CreateFile();
            var job = await _nexusModsLibrary.CreateDownloadJob(tempPath, newestFile, cancellationToken: cancellationToken);
            await _libraryService.AddDownload(job);
        }
    }

    private ValueTask HandleViewChangelogMessage(ViewChangelogMessage viewChangelogMessage, CancellationToken cancellationToken)
    {
/*
Note(sewer): This method currently sends you to the mod page due to technical limitations.
A summarised quote/explanation from myself below:

- On the site, there is no true 'per file' level changelog, but only a changelog at the mod page level currently.
    - The widget which shows the changes e.g. https://www.nexusmods.com/Core/Libs/Common/Widgets/ModChangeLogPopUp?mod_id=2347&game_id=1704&version=10.0.1 
      cannot be explicitly opened via hyperlink (due to a recent change on the site to block this).
    - From observation, I think those are automatically added when the user sets the version of the file to be the 
      same as the version of a changelog item made on the mod page.
- For rendering it myself: GraphQL API has a `changelogText` field for mod files.
    - When present, the values are listed in the form `#<ModChangelog:0x00007f060e21e880>` .
    - But there's no ModChangelog type or field anywhere in the autogenerated API docs.
    - I imagine you can manually hack this together by using V1 API, but code would be quickly obsolete.
- Some mod authors put the changelog in the file description; rather than on page level changelog.

After asking design, we're choosing to simply open the mod page for now.
*/
        return viewChangelogMessage.Id.Match(
            modPageMetadataId => OpenModPage(modPageMetadataId, cancellationToken),
            libraryItemId => OpenModPage(new NexusModsLibraryItem.ReadOnly(_connection.Db, libraryItemId).ModPageMetadataId, cancellationToken)
        );
    }
    
    private ValueTask HandleViewModPageMessage(ViewModPageMessage viewModPageMessage, CancellationToken cancellationToken)
    {
        return viewModPageMessage.Id.Match(
            modPageMetadataId => OpenModPage(modPageMetadataId, cancellationToken),
            libraryItemId => OpenModPage(new NexusModsLibraryItem.ReadOnly(_connection.Db, libraryItemId).ModPageMetadataId, cancellationToken)
        );
    }
    
    private async ValueTask HandleDeleteItemMessage(DeleteItemMessage deleteItemMessage, CancellationToken cancellationToken)
    {
        var ids = deleteItemMessage.Ids;
        if (ids.Length == 0) return;
        
        var toRemove = ids.Select(id => LibraryItem.Load(_connection.Db, id)).ToArray();
        await LibraryItemRemover.RemoveAsync(_connection, _serviceProvider.GetRequiredService<IOverlayController>(), _libraryService, toRemove);

        _notificationService.ShowToast(Language.ToastNotification_Items_deleted);
    }
    
    private async ValueTask HandleHideUpdatesMessage(HideUpdatesMessage hideUpdatesMessage, CancellationToken cancellationToken)
    {
        var modUpdateFilterService = _serviceProvider.GetRequiredService<IModUpdateFilterService>();

        await hideUpdatesMessage.Id.Match(
            async modPageId =>
            {
                // Handle hiding/showing updates for a mod page
                // First get all library items we have that come from the mod page.
                var allLibraryFilesForThisMod =  NexusModsLibraryItem.All(_connection.Db)
                    .Where(x => x.ModPageMetadataId == modPageId)
                    .Select(x => x.FileMetadata)
                    .ToArray();

                // Note(sewer):
                // Behaviour per captainsandypants (Slack).
                // 'If any children have updates set to hidden, then the parent should have "Show updates" as the menu item.
                // When selected, this will set all children to show updates.'
                //
                // We have to be careful here.
                // We have a list of ***current versions*** of files in the mod page.
                // We need to check if it's possible to update ***any of these current versions*** to a new file.
                //     👉 👉 So for each file we need to check if any of its versions is not ignored as an update.
                var modsWithUpdatesHidden = allLibraryFilesForThisMod.Where(x =>
                    {
                        // Checking all versions is not a bug, it is intended behaviour per design.
                        // Search 'That definition also means older versions should be excluded from update checks.' in Slack.
                        var newerFiles = RunUpdateCheck.GetAllVersionsForExistingFile(x).ToArray();
                        return newerFiles.All(newer => modUpdateFilterService.IsFileHidden(newer.Uid));
                    }
                ).ToArray();

                if (modsWithUpdatesHidden.Length > 0)
                {
                    var allVersionsOfLibraryItemsWithUpdatesHidden =
                        modsWithUpdatesHidden.SelectMany(RunUpdateCheck.GetAllVersionsForExistingFile)
                                             .Select(x => x.Uid);
                    await modUpdateFilterService.ShowFilesAsync(allVersionsOfLibraryItemsWithUpdatesHidden);
                }
                else
                {
                    var allVersionsOfAllFilesForThisMod = allLibraryFilesForThisMod
                        .SelectMany(RunUpdateCheck.GetAllVersionsForExistingFile)
                        .Select(x => x.Uid);
                    await modUpdateFilterService.HideFilesAsync(allVersionsOfAllFilesForThisMod);
                }
            },
            async libraryItemId =>
            {
                // Handle hiding/showing updates for a single file
                var libraryItem = NexusModsLibraryItem.Load(_connection.Db, libraryItemId);
                var fileMetadata = libraryItem.FileMetadata;
                var allVersions = RunUpdateCheck.GetAllVersionsForExistingFile(fileMetadata).ToArray();

                var areAllHidden = allVersions.All(x => modUpdateFilterService.IsFileHidden(x.Uid));

                if (areAllHidden)
                    await modUpdateFilterService.ShowFilesAsync(allVersions.Select(x => x.Uid));
                else
                    await modUpdateFilterService.HideFilesAsync(allVersions.Select(x => x.Uid));
            }
        );
    }

    private ValueTask OpenModPage(NexusModsModPageMetadataId modPageMetadataId, CancellationToken cancellationToken)
    {
        var modPage = new NexusModsModPageMetadata.ReadOnly(_connection.Db, modPageMetadataId);
        var url = NexusModsUrlBuilder.GetModUri(modPage.GameDomain, modPage.Uid.ModId);
        var os = _serviceProvider.GetRequiredService<IOSInterop>();
        // Note(sewer): Don't await, we don't want to block the UI thread when user pops a webpage.
        _ = os.OpenUrl(url, cancellationToken: cancellationToken);
        return ValueTask.CompletedTask;
    }

    // Note(sewer): ValueTask because of R3 constraints with ReactiveCommand API
    private async ValueTask RefreshUpdates(CancellationToken token) 
    {
        await _modUpdateService.CheckAndUpdateModPages(token, notify: true);
    }

    private async ValueTask InstallItems(LibraryItemId[] ids, LoadoutItemGroupId targetLoadoutGroup, bool useAdvancedInstaller, CancellationToken cancellationToken)
    {
        var db = _connection.Db;
        var items = ids
            .Select(id => LibraryItem.Load(db, id))
            .Where(x => x.IsValid())
            .ToArray();

        await Parallel.ForAsync(
            fromInclusive: 0,
            toExclusive: items.Length,
            body: (i, innerCancellationToken) => InstallLibraryItem(items[i], _loadout, targetLoadoutGroup, innerCancellationToken, useAdvancedInstaller),
            cancellationToken: cancellationToken
        );
        
        var targetCollection = LoadoutItem.Load(db, targetLoadoutGroup);
        _notificationService.ShowToast(string.Format(Language.ToastNotification_Installed_to__0_, targetCollection.Name));
    }

    private LibraryItemId[] GetSelectedIds()
    {
        var ids = Adapter.SelectedModels
            .SelectMany(static model => GetLibraryItemIds(model))
            .Distinct()
            .ToArray();

        return ids;
    }
    
    private static IEnumerable<LibraryItemId> GetLibraryItemIds(CompositeItemModel<EntityId> itemModel)
    {
        return itemModel.Get<LibraryComponents.LibraryItemIds>(LibraryColumns.Actions.LibraryItemIdsComponentKey).ItemIds;
    }

    private IEnumerable<CompositeItemModel<EntityId>> GetSelectedModelsWithUpdates()
    {
        return Adapter.SelectedModels
            .Where(model =>
            {
                var updateComponent = model.GetOptional<LibraryComponents.UpdateAction>(LibraryColumns.Actions.UpdateComponentKey);
                return updateComponent.HasValue && updateComponent.Value.NewFiles.Value.HasAnyUpdates;
            });
    }

    private LoadoutItemGroupId GetInstallationTarget() => (SelectedInstallationTarget?.Id ?? _installationTargets[0].Id).Value;

    private ValueTask InstallSelectedItems(bool useAdvancedInstaller, CancellationToken cancellationToken)
    {
        return InstallItems(GetSelectedIds(), GetInstallationTarget(), useAdvancedInstaller, cancellationToken);
    }

    private async ValueTask InstallLibraryItem(
        LibraryItem.ReadOnly libraryItem,
        LoadoutId loadout,
        LoadoutItemGroupId targetLoadoutGroup,
        CancellationToken cancellationToken,
        bool useAdvancedInstaller = false)
    {
        try
        {
            await _libraryService.InstallItem(libraryItem, loadout, parent: targetLoadoutGroup, installer: useAdvancedInstaller ? _advancedInstaller : null);
            
            var targetCollection  = LoadoutItem.Load(_connection.Db, targetLoadoutGroup);
            _notificationService.ShowToast(string.Format(Language.ToastNotification_Installed_to__0_, targetCollection.Name));
        }
        catch (OperationCanceledException)
        {
            // User cancelled the installation - this is expected behavior, don't show error
            var logger = _serviceProvider.GetRequiredService<ILogger<LibraryViewModel>>();
            logger.LogInformation("Installation of {LibraryItem} was cancelled by user", libraryItem.Name);
        }
    }

    private async ValueTask RemoveSelectedItems(CancellationToken cancellationToken)
    {
        var db = _connection.Db;
        var toRemove = GetSelectedIds().Select(id => LibraryItem.Load(db, id)).ToArray();
        await LibraryItemRemover.RemoveAsync(_connection, _serviceProvider.GetRequiredService<IOverlayController>(), _libraryService, toRemove);
        
        _notificationService.ShowToast(Language.ToastNotification_Items_deleted);
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

    private ValueTask UpdateSelectedItems(CancellationToken cancellationToken)
    {
        return UpdateSelectedItemsInternal(useUpdateAndReplace: true, cancellationToken);
    }

    private ValueTask UpdateAndKeepOldSelectedItems(CancellationToken cancellationToken)
    {
        return UpdateSelectedItemsInternal(useUpdateAndReplace: false, cancellationToken);
    }

    private async ValueTask UpdateSelectedItemsInternal(bool useUpdateAndReplace, CancellationToken cancellationToken)
    {
        var selectedModels = Adapter.SelectedModels.ToArray();
        // Note(sewer): The selection count is not guaranteed to equal the number of items
        // with updates because the user can select a mixture of items with and without updates
        // in a single selection.
        var withUpdatesOnPage = new List<ModUpdatesOnModPage>(); 
        
        foreach (var model in selectedModels)
        {
            // Check if this model has an update component
            var updateComponent = model.GetOptional<LibraryComponents.UpdateAction>(LibraryColumns.Actions.UpdateComponentKey);
            if (!updateComponent.HasValue) continue;

            var component = updateComponent.Value;
            var updatesOnPage = component.NewFiles.Value;
            
            if (!updatesOnPage.HasAnyUpdates) continue;
            withUpdatesOnPage.Add(updatesOnPage);
        }
        
        // Note(sewer): This should (by definition) be always true, because the user had at least
        // 1 selection with an update (otherwise they wouldn't be able to call this)
        if (withUpdatesOnPage.Count > 0)
        {
            var isPremium = _loginManager.IsPremium;
            if (useUpdateAndReplace)
            {
                if (!isPremium)
                    await UpdateAndReplaceForMultiModPagesFreeOnly(cancellationToken, withUpdatesOnPage);
                else
                    await UpdateAndReplaceForMultiModPagesPremiumOnly(cancellationToken, withUpdatesOnPage);
            }
            else
            {
                if (!isPremium)
                    await UpdateAndKeepOldFree(withUpdatesOnPage, cancellationToken);
                else
                    await UpdateAndKeepOldPremium(withUpdatesOnPage, cancellationToken);
            }
        }
    }

    private async ValueTask UpdateAllItems(CancellationToken cancellationToken)
    {
        IsUpdatingAll = true;
        try
        {
            var isPremium = _loginManager.IsPremium;

            if (!isPremium)
            {
                var osInterop = _serviceProvider.GetRequiredService<IOSInterop>();
                await PremiumDialog.ShowUpdatePremiumDialog(WindowManager, osInterop);
                return;
            }

            // Filter mod pages to only those for the current game
            var currentGameId = _loadout.InstallationInstance.Game.GameId;
            var modPagesWithUpdates = _modUpdateService.GetAllModPagesWithUpdates()
                .Where(pair => 
                {
                    var modPage = NexusModsModPageMetadata.Load(_connection.Db, pair.modPageId);
                    return modPage.IsValid() && modPage.Uid.GameId.Equals(currentGameId);
                });
            var allUpdates = modPagesWithUpdates.Select(pair => pair.updates).ToArray();
            
            if (allUpdates.Length > 0)
                await UpdateAndReplaceForMultiModPagesPremiumOnly(cancellationToken, allUpdates);
        }
        finally
        {
            IsUpdatingAll = false;
        }
    }

    /// <summary>
    /// Removes old library items that are not installed in any read-only (e.g. Nexus) collections.
    /// This ensures collections 'remain' installed.
    /// 
    /// Old items where the new library item is invalid (e.g. Replace Failed), are preserved;
    /// cleaning those up happens in <see cref="ShowSomeInstallsFailedDialog"/> in partial fail scenarios.
    /// </summary>
    /// <param name="oldToNewLibraryMapping">The mapping of old library items to their new replacements.</param>
    private async ValueTask RemoveOldLibraryItemsIfNotInReadOnlyCollections(IEnumerable<DownloadedProcessingResult> oldToNewLibraryMapping)
    {
        var oldItemsToRemove = new List<LibraryItem.ReadOnly>();
        
        foreach (var item in oldToNewLibraryMapping)
        {
            // If the new item is not valid (e.g. replace failed), and user chose to 'delete new files' we skip it.
            if (!item.NewItem.IsValid())
                continue;
            
            // Check if the old item is still linked to any collections
            var collectionsWithItem = _libraryService.CollectionsWithLibraryItem(item.OldItem, excludeReadOnlyCollections: false);
            
            // Only remove if the item is NOT in any read-only collections (i.e., only in modifiable collections or no collections)
            var hasReadOnlyCollections = collectionsWithItem.Any(x => x.collection.IsReadOnly);
            if (!hasReadOnlyCollections)
                oldItemsToRemove.Add(item.OldItem);
        }

        if (oldItemsToRemove.Count > 0)
            await _libraryService.RemoveLibraryItems(oldItemsToRemove);
    }
}

public readonly record struct InstallMessage(LibraryItemId[] Ids);
public readonly record struct UpdateAndReplaceMessage(ModUpdatesOnModPage Updates, CompositeItemModel<EntityId> TreeNode);
public readonly record struct UpdateAndKeepOldMessage(ModUpdatesOnModPage Updates, CompositeItemModel<EntityId> TreeNode);
public readonly record struct ViewChangelogMessage(OneOf<NexusModsModPageMetadataId, NexusModsLibraryItemId> Id);
public readonly record struct ViewModPageMessage(OneOf<NexusModsModPageMetadataId, NexusModsLibraryItemId> Id);
public readonly record struct HideUpdatesMessage(OneOf<NexusModsModPageMetadataId, NexusModsLibraryItemId> Id);
public readonly record struct DeleteItemMessage(LibraryItemId[] Ids);

public class LibraryTreeDataGridAdapter :
    TreeDataGridAdapter<CompositeItemModel<EntityId>, EntityId>,
    ITreeDataGirdMessageAdapter<OneOf<InstallMessage, UpdateAndReplaceMessage, UpdateAndKeepOldMessage, ViewChangelogMessage, ViewModPageMessage, HideUpdatesMessage, DeleteItemMessage>>
{
    private readonly ILibraryDataProvider[] _libraryDataProviders;
    private readonly LibraryFilter _libraryFilter;
    private readonly IConnection _connection;

    public Subject<OneOf<InstallMessage, UpdateAndReplaceMessage, UpdateAndKeepOldMessage, ViewChangelogMessage, ViewModPageMessage, HideUpdatesMessage, DeleteItemMessage>> MessageSubject { get; } = new();

    public LibraryTreeDataGridAdapter(IServiceProvider serviceProvider, LibraryFilter libraryFilter)
    {
        _libraryFilter = libraryFilter;
        _libraryDataProviders = serviceProvider.GetServices<ILibraryDataProvider>().ToArray();
        _connection = serviceProvider.GetRequiredService<IConnection>();
    }

    protected override IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> GetRootsObservable(bool viewHierarchical)
    {
        return _libraryDataProviders.Select(x => x.ObserveLibraryItems(_libraryFilter)).MergeChangeSets();
    }

    protected override void BeforeModelActivationHook(CompositeItemModel<EntityId> model)
    {
        base.BeforeModelActivationHook(model);

        model.SubscribeToComponentAndTrack<LibraryComponents.InstallAction, LibraryTreeDataGridAdapter>(
            key: LibraryColumns.Actions.InstallComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandInstall.Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, model, component) = state;
                var ids = GetLibraryItemIds(model).ToArray();

                self.MessageSubject.OnNext(new InstallMessage(ids));
            })
        );

        model.SubscribeToComponentAndTrack<LibraryComponents.UpdateAction, LibraryTreeDataGridAdapter>(
            key: LibraryColumns.Actions.UpdateComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandUpdateAndKeepOld
                .SubscribeOnUIThreadDispatcher() // Update payload may expand row for free users, requiring UI thread.
                .Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, model, component) = state;
                var newFile = component.NewFiles.Value;
                self.MessageSubject.OnNext(new UpdateAndKeepOldMessage(newFile, model));
            })
        );

        model.SubscribeToComponentAndTrack<LibraryComponents.UpdateAction, LibraryTreeDataGridAdapter>(
            key: LibraryColumns.Actions.UpdateComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandUpdateAndReplace
                .SubscribeOnUIThreadDispatcher() // Update payload may expand row for free users, requiring UI thread.
                .Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, model, component) = state;
                var newFile = component.NewFiles.Value;
                self.MessageSubject.OnNext(new UpdateAndReplaceMessage(newFile, model));
            })
        );

        static OneOf<NexusModsModPageMetadataId, NexusModsLibraryItemId> GetModPageIdOneOfType(IDb db, EntityId entityId)
        {
            var modPageMetadata = NexusModsModPageMetadata.Load(db, entityId);
            if (modPageMetadata.IsValid())
                return OneOf<NexusModsModPageMetadataId, NexusModsLibraryItemId>.FromT0(modPageMetadata.Id);
                
            // Try to load as NexusModsLibraryItem
            var libraryItem = NexusModsLibraryItem.Load(db, entityId);
            if (libraryItem.IsValid())
                return OneOf<NexusModsModPageMetadataId, NexusModsLibraryItemId>.FromT1(libraryItem.Id);
            
            throw new Exception("Unknown type of entity for ViewChangelogAction: " + entityId);
        }

        model.SubscribeToComponentAndTrack<LibraryComponents.ViewChangelogAction, LibraryTreeDataGridAdapter>(
            key: LibraryColumns.Actions.ViewChangelogComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandViewChangelog.Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, model, _) = state;
                var entityId = model.Key;
                
                self.MessageSubject.OnNext(new ViewChangelogMessage(GetModPageIdOneOfType(self._connection.Db, entityId)));
            })
        );

        model.SubscribeToComponentAndTrack<LibraryComponents.ViewModPageAction, LibraryTreeDataGridAdapter>(
            key: LibraryColumns.Actions.ViewModPageComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandViewModPage.Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, model, _) = state;
                var entityId = model.Key;

                self.MessageSubject.OnNext(new ViewModPageMessage(GetModPageIdOneOfType(self._connection.Db, entityId)));
            })
        );
        
        model.SubscribeToComponentAndTrack<LibraryComponents.DeleteItemAction, LibraryTreeDataGridAdapter>(
            key: LibraryColumns.Actions.DeleteItemComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandDeleteItem.Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, model, component) = state;
                var ids = GetLibraryItemIds(model).ToArray();

                self.MessageSubject.OnNext(new DeleteItemMessage(ids));
            })
        );

        model.SubscribeToComponentAndTrack<LibraryComponents.HideUpdatesAction, LibraryTreeDataGridAdapter>(
            key: LibraryColumns.Actions.HideUpdatesComponentKey,
            state: this,
            factory: static (self, itemModel, component) => component.CommandHideUpdates.Subscribe((self, itemModel, component), static (_, state) =>
            {
                var (self, model, _) = state;
                var entityId = model.Key;

                self.MessageSubject.OnNext(new HideUpdatesMessage(GetModPageIdOneOfType(self._connection.Db, entityId)));
            })
        );
    }
    
    private static IEnumerable<LibraryItemId> GetLibraryItemIds(CompositeItemModel<EntityId> itemModel)
    {
        return itemModel.Get<LibraryComponents.LibraryItemIds>(LibraryColumns.Actions.LibraryItemIdsComponentKey).ItemIds;
    }

    protected override IColumn<CompositeItemModel<EntityId>>[] CreateColumns(bool viewHierarchical)
    {
        var nameColumn = ColumnCreator.Create<EntityId, SharedColumns.Name>();

        return
        [
            viewHierarchical ? ITreeDataGridItemModel<CompositeItemModel<EntityId>, EntityId>.CreateExpanderColumn(nameColumn) : nameColumn,
            ColumnCreator.Create<EntityId, LibraryColumns.ItemVersion>(),
            ColumnCreator.Create<EntityId, SharedColumns.ItemSize>(),
            ColumnCreator.Create<EntityId, LibraryColumns.DownloadedDate>(sortDirection: ListSortDirection.Descending),
            ColumnCreator.Create<EntityId, SharedColumns.InstalledDate>(),
            ColumnCreator.Create<EntityId, LibraryColumns.Actions>(),
        ];
    }

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            MessageSubject.Dispose();
            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// Results from processing downloads in parallel.
/// </summary>
/// <param name="LibraryItemReplacementResults">
///     Mapping of all the old mods to new mods; along with their replacement result.
///     This excludes items where downloads failed.
/// </param>
/// <param name="DownloadErrors">Items which failed to download.</param>
public readonly record struct DownloadProcessingResults(
    ConcurrentBag<DownloadedProcessingResult> LibraryItemReplacementResults,
    ConcurrentBag<DownloadError> DownloadErrors
);

/// <summary>
/// The result of downloading and installing a given old->new mod tuple.
/// </summary>
public struct DownloadedProcessingResult
{
    /// <summary>
    /// The old version of the mod.
    /// </summary>
    public LibraryItem.ReadOnly OldItem;

    /// <summary>
    /// The new version of the mod that was downloaded.
    /// </summary>
    /// <remarks>
    ///     This item may be invalid if the user decides to delete the file when <see cref="InstallResult"/>
    ///     is not success.
    /// </remarks>
    public LibraryItem.ReadOnly NewItem;
    
    /// <summary>
    /// The install result for <see cref="NewItem"/>.
    /// </summary>
    public LibraryItemReplacementResult InstallResult;
}

/// <summary>
/// An error which happened when downloading a new version of a library item.
/// </summary>
public struct DownloadError
{
    /// <summary>
    /// The file which we tried to download.
    /// </summary>
    public NexusModsFileMetadata.ReadOnly File;
    
    /// <summary>
    /// The error which occurred during the download or installation of the file.
    /// </summary>
    public Exception Error;
}
