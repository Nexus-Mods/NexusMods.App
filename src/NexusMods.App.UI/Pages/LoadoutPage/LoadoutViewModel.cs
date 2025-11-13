using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Sdk.Settings;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Pages.LoadoutGroupFilesPage;
using NexusMods.App.UI.Pages.LoadoutPage.Dialogs.CollectionPublished;
using NexusMods.App.UI.Pages.LoadoutPage.Dialogs.ShareCollection;
using NexusMods.App.UI.Pages.Sorting;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Settings;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Collections;
using NexusMods.UI.Sdk.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Sdk;
using NexusMods.Sdk.Loadouts;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Dialog;
using NexusMods.UI.Sdk.Dialog.Enums;
using ObservableCollections;
using R3;
using ReactiveUI;
using CollectionStatus = NexusMods.Abstractions.NexusModsLibrary.Models.CollectionStatus;
using ReactiveCommand = R3.ReactiveCommand;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public class LoadoutViewModel : APageViewModel<ILoadoutViewModel>, ILoadoutViewModel
{
    public string EmptyStateTitleText { get; }

    public LoadoutTreeDataGridAdapter Adapter { get; }
    private BindableReactiveProperty<int> ItemCount { get; } = new();
    IReadOnlyBindableReactiveProperty<int> ILoadoutViewModel.ItemCount => ItemCount;
    private BindableReactiveProperty<int> SelectionCount { get; } = new();
    IReadOnlyBindableReactiveProperty<int> ILoadoutViewModel.SelectionCount => SelectionCount;

    public LoadoutPageSubTabs SelectedSubTab { get; }
    public bool HasRulesSection { get; }
    public ISortingSelectionViewModel RulesSectionViewModel { get; }

    public bool IsCollection { get; }
    private BindableReactiveProperty<string> CollectionName { get; }
    IReadOnlyBindableReactiveProperty<string> ILoadoutViewModel.CollectionName => CollectionName;
    private BindableReactiveProperty<bool> IsCollectionUploaded { get; }
    IReadOnlyBindableReactiveProperty<bool> ILoadoutViewModel.IsCollectionUploaded => IsCollectionUploaded;

    private BindableReactiveProperty<CollectionStatus> CollectionStatus { get; }
    IReadOnlyBindableReactiveProperty<CollectionStatus> ILoadoutViewModel.CollectionStatus => CollectionStatus;
    private BindableReactiveProperty<RevisionStatus> RevisionStatus { get; }
    IReadOnlyBindableReactiveProperty<RevisionStatus> ILoadoutViewModel.RevisionStatus => RevisionStatus;
    private BindableReactiveProperty<RevisionNumber> RevisionNumber { get; }
    IReadOnlyBindableReactiveProperty<RevisionNumber> ILoadoutViewModel.RevisionNumber => RevisionNumber;
    private BindableReactiveProperty<DateTimeOffset> LastUploadedDate { get; }
    IReadOnlyBindableReactiveProperty<DateTimeOffset> ILoadoutViewModel.LastUploadedDate => LastUploadedDate;
    private BindableReactiveProperty<bool> HasOutstandingChanges { get; } = new(value: true);
    IReadOnlyBindableReactiveProperty<bool> ILoadoutViewModel.HasOutstandingChanges => HasOutstandingChanges;

    public ReactiveCommand<NavigationInformation> CommandOpenLibraryPage { get; }
    public ReactiveCommand<NavigationInformation> CommandOpenFilesPage { get; }

    public ReactiveCommand<Unit> CommandRemoveItem { get; }
    public ReactiveCommand<Unit> CommandDeselectItems { get; }

    public ReactiveCommand<Unit> CommandRenameGroup { get; }
    public ReactiveCommand<Unit> CommandShareCollection { get; }
    public ReactiveCommand<Unit> CommandUploadDraftRevision { get; }
    public ReactiveCommand<Unit> CommandUploadAndPublishRevision { get; }
    public ReactiveCommand<Unit> CommandOpenRevisionUrl { get; }
    public ReactiveCommand<Unit> CommandCopyRevisionUrl { get; }
    public ReactiveCommand<Unit> CommandChangeVisibility { get; }
    public ReactiveCommand<Unit> CommandDeleteGroup { get; }

    public bool EnableCollectionSharing { get; }

    private readonly IServiceProvider _serviceProvider;
    private readonly NexusModsLibrary _nexusModsLibrary;
    private readonly IConnection _connection;
    private readonly IAvaloniaInterop _avaloniaInterop;
    private readonly IWindowNotificationService _notificationService;
    private readonly ILoadoutManager _loadoutManager;

    public LoadoutViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        LoadoutId loadoutId,
        Optional<CollectionGroupId> collectionGroupId = default,
        Optional<LoadoutPageSubTabs> selectedSubTab = default) : base(windowManager)
    {
        _serviceProvider = serviceProvider;
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _nexusModsLibrary = serviceProvider.GetRequiredService<NexusModsLibrary>();
        _avaloniaInterop = serviceProvider.GetRequiredService<IAvaloniaInterop>();
        _notificationService = serviceProvider.GetRequiredService<IWindowNotificationService>();
        _loadoutManager = serviceProvider.GetRequiredService<ILoadoutManager>();

        var settingsManager = serviceProvider.GetRequiredService<ISettingsManager>();
        EnableCollectionSharing = settingsManager.Get<ExperimentalSettings>().EnableCollectionSharing;

        var loadoutFilter = new LoadoutFilter
        {
            LoadoutId = loadoutId,
            CollectionGroupId = collectionGroupId.Convert(x => (LoadoutItemGroupId)x.Value),
        };

        Adapter = new LoadoutTreeDataGridAdapter(serviceProvider, loadoutFilter);

        if (collectionGroupId.HasValue)
        {
            var collectionGroup = LoadoutItem.Load(_connection.Db, collectionGroupId.Value);
            IsCollection = true;
            TabTitle = collectionGroup.Name;
            TabIcon = IconValues.CollectionsOutline;

            CollectionName = new BindableReactiveProperty<string>(value: collectionGroup.Name);

            if (ManagedCollectionLoadoutGroup.Load(_connection.Db, collectionGroupId.Value).IsValid())
            {
                IsCollectionUploaded = new BindableReactiveProperty<bool>(value: true);

                var managed = ManagedCollectionLoadoutGroup.Load(_connection.Db, collectionGroupId.Value);
                CollectionStatus = new BindableReactiveProperty<CollectionStatus>(value: managed.Collection.Status.ValueOr(() => Abstractions.NexusModsLibrary.Models.CollectionStatus.Unlisted));
                RevisionStatus = new BindableReactiveProperty<RevisionStatus>(value: managed.ToStatus());
                RevisionNumber = new BindableReactiveProperty<RevisionNumber>(value: managed.CurrentRevisionNumber);
                LastUploadedDate = new BindableReactiveProperty<DateTimeOffset>(value: managed.LastUploadDate);
            }
            else
            {
                IsCollectionUploaded = new BindableReactiveProperty<bool>(value: false);
                CollectionStatus = new BindableReactiveProperty<CollectionStatus>(value: Abstractions.NexusModsLibrary.Models.CollectionStatus.Unlisted);
                RevisionStatus = new BindableReactiveProperty<RevisionStatus>(value: Abstractions.NexusModsLibrary.Models.RevisionStatus.Draft);
                RevisionNumber = new BindableReactiveProperty<RevisionNumber>(value: Abstractions.NexusWebApi.Types.RevisionNumber.From(1));
                LastUploadedDate = new BindableReactiveProperty<DateTimeOffset>(value: DateTimeOffset.UtcNow);
            }

            // If there are no other collections in the loadout, this is the `My Mods` collection and `All` view is hidden,
            // so we show the `sorting views here` view here instead
            var isSingleCollectionObservable = CollectionGroup.ObserveAll(_connection)
                .Filter(collection => collection.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == loadoutId)
                .Transform(collection => collection.Id)
                .QueryWhenChanged(collections => collections.Count == 1)
                .ToObservable();

            RulesSectionViewModel = new SortingSelectionViewModel(serviceProvider, windowManager, loadoutId,
                canEditObservable: isSingleCollectionObservable
            );

            CommandShareCollection = Adapter.IsSourceEmpty.Select(b => !b).ToReactiveCommand<Unit>(async (unit, cancellationToken) =>
                {
                    // pass in current collection status
                    var shareViewModel = new DialogShareCollectionViewModel(CollectionStatus.Value == Abstractions.NexusModsLibrary.Models.CollectionStatus.Listed);

                    var shareDialog = DialogFactory.CreateDialog("Choose How to Share Your Collection",
                        [
                            new DialogButtonDefinition(
                                "Cancel",
                                ButtonDefinitionId.Cancel,
                                ButtonAction.Reject
                            ),
                            new DialogButtonDefinition(
                                "Publish",
                                ButtonDefinitionId.Accept,
                                ButtonAction.Accept,
                                ButtonStyling.Primary
                            ),
                        ],
                        shareViewModel
                    );

                    var shareDialogResult = await windowManager.ShowDialog(shareDialog, DialogWindowType.Modal);

                    if (shareDialogResult.ButtonId != ButtonDefinitionId.Accept) return;

                    // Publish has been clicked, so we upload the collection with the new IsListed status
                    var initialCollectionStatus = shareViewModel.IsListed
                        ? Abstractions.NexusModsLibrary.Models.CollectionStatus.Listed
                        : Abstractions.NexusModsLibrary.Models.CollectionStatus.Unlisted;

                    var collection = await CollectionCreator.CreateCollection(serviceProvider, collectionGroupId.Value, initialCollectionStatus,
                        cancellationToken
                    );

                    IsCollectionUploaded.Value = true;
                    HasOutstandingChanges.Value = false;

                    // now we have uploaded the collection, we can show the success dialog

                    // strip out querystring from uri so we don't show it in the UI
                    var collectionUriWithoutQuery = new UriBuilder(GetCollectionUri(collection))
                    {
                        Query = string.Empty,
                    };

                    // pass in current collection status and collection url
                    var collectionPublishedViewModel = new DialogCollectionPublishedViewModel(
                        collection.Name,
                        collection.Status.Value,
                        collectionUriWithoutQuery.Uri,
                        serviceProvider,
                        true
                    );

                    var collectionPublishedDialog = DialogFactory.CreateDialog("Your Collection is Now Published!",
                        [
                            new DialogButtonDefinition(
                                "Close",
                                ButtonDefinitionId.Close,
                                ButtonAction.Reject
                            ),
                            new DialogButtonDefinition(
                                "View Page",
                                ButtonDefinitionId.Accept,
                                ButtonAction.Accept,
                                ButtonStyling.Default,
                                IconValues.OpenInNew
                            ),
                        ],
                        collectionPublishedViewModel
                    );

                    var collectionPublishedResult = await windowManager.ShowDialog(collectionPublishedDialog, DialogWindowType.Modal);

                    if (collectionPublishedResult.ButtonId != ButtonDefinitionId.Accept) return;

                    var uri = GetCollectionUri(collection);
                    serviceProvider.GetRequiredService<IOSInterop>().OpenUri(uri);
                }
            );

            CommandUploadDraftRevision = IsCollectionUploaded.ToReactiveCommand<Unit>(async (unit, cancellationToken) =>
                {
                    _notificationService.ShowToast(Language.ToastNotification_Uploading_draft_collection_revision___);
                    
                    _ = await CollectionCreator.UploadDraftRevision(serviceProvider, collectionGroupId.Value.Value, cancellationToken);
                    HasOutstandingChanges.Value = false;
                    
                    _notificationService.ShowToast(Language.ToastNotification_Draft_revision_uploaded_successfully, ToastNotificationVariant.Success);
                }, maxSequential: 1, configureAwait: false
            );

            CommandUploadAndPublishRevision = IsCollectionUploaded.ToReactiveCommand<Unit>(async (unit, cancellationToken) =>
                {
                    _notificationService.ShowToast(Language.ToastNotification_Uploading_new_collection_revision___);
                    
                    _ = await CollectionCreator.UploadAndPublishRevision(serviceProvider, collectionGroupId.Value.Value, cancellationToken);
                    HasOutstandingChanges.Value = false;

                    var managedCollectionLoadoutGroup = ManagedCollectionLoadoutGroup.Load(_connection.Db, collectionGroupId.Value);
                    if (!managedCollectionLoadoutGroup.IsValid()) return;

                    // strip out querystring from uri so we don't show it in the UI
                    var collectionUriWithoutQuery = new UriBuilder(GetCollectionUri(managedCollectionLoadoutGroup.Collection))
                    {
                        Query = string.Empty,
                    };

                    // pass in current collection status and collection url
                    var collectionPublishedViewModel = new DialogCollectionPublishedViewModel(
                        managedCollectionLoadoutGroup.Collection.Name,
                        managedCollectionLoadoutGroup.Collection.Status.Value,
                        collectionUriWithoutQuery.Uri,
                        serviceProvider
                    );

                    var collectionPublishedDialog = DialogFactory.CreateDialog($"Revision {managedCollectionLoadoutGroup.LastPublishedRevisionNumber.Value} is Now Published!",
                        [
                            new DialogButtonDefinition(
                                "Close",
                                ButtonDefinitionId.Close,
                                ButtonAction.Reject
                            ),
                            new DialogButtonDefinition(
                                "Update Changelog",
                                ButtonDefinitionId.Accept,
                                ButtonAction.Accept,
                                ButtonStyling.Primary,
                                IconValues.OpenInNew
                            ),
                        ],
                        collectionPublishedViewModel
                    );

                    var collectionPublishedResult = await windowManager.ShowDialog(collectionPublishedDialog, DialogWindowType.Modal);

                    if (collectionPublishedResult.ButtonId != ButtonDefinitionId.Accept) return;

                    // open up changelog URL in browser
                    var uri = GetCollectionChangelogUri(managedCollectionLoadoutGroup.Collection);
                    serviceProvider.GetRequiredService<IOSInterop>().OpenUri(uri);
                }, maxSequential: 1, configureAwait: false
            );

            CommandChangeVisibility = new ReactiveCommand<Unit>(async (unit, cancellationToken) =>
                {
                    // pass in current collection status
                    var shareViewModel = new DialogShareCollectionViewModel(CollectionStatus.Value == Abstractions.NexusModsLibrary.Models.CollectionStatus.Listed);

                    var shareDialog = DialogFactory.CreateDialog("Visibility Settings for Your Collection",
                        [
                            new DialogButtonDefinition(
                                "Cancel",
                                ButtonDefinitionId.Cancel,
                                ButtonAction.Reject
                            ),
                            new DialogButtonDefinition(
                                "Save Changes",
                                ButtonDefinitionId.Accept,
                                ButtonAction.Accept,
                                ButtonStyling.Primary
                            ),
                        ],
                        shareViewModel
                    );

                    var shareDialogResult = await windowManager.ShowDialog(shareDialog, DialogWindowType.Modal);

                    if (shareDialogResult.ButtonId == ButtonDefinitionId.Cancel) return;

                    // save the changes to the collection
                    CollectionStatus.Value = shareViewModel.IsListed
                        ? Abstractions.NexusModsLibrary.Models.CollectionStatus.Listed
                        : Abstractions.NexusModsLibrary.Models.CollectionStatus.Unlisted;

                    var result = await CollectionCreator.ChangeCollectionStatus(serviceProvider, collectionGroupId.Value.Value, CollectionStatus.Value,
                        cancellationToken
                    );
                    
                    if (result.TryGetData(out var data))
                    {
                        var newStatus = data switch
                        {
                            Abstractions.NexusModsLibrary.Models.CollectionStatus.Listed => Language.CollectionStatus_Listed,
                            Abstractions.NexusModsLibrary.Models.CollectionStatus.Unlisted => Language.CollectionStatus_Unlisted,
                            _ => throw new ArgumentOutOfRangeException(),
                        };

                        _notificationService.ShowToast(
                            string.Format(Language.ToastNotification_Collection_status_changed_to__0__, newStatus),
                            ToastNotificationVariant.Success
                        );
                    }
                }, configureAwait: false
            );

            CommandOpenRevisionUrl = IsCollectionUploaded.ToReactiveCommand<Unit>(async (_, cancellationToken) =>
                {
                    var managedCollectionLoadoutGroup = ManagedCollectionLoadoutGroup.Load(_connection.Db, collectionGroupId.Value);
                    if (!managedCollectionLoadoutGroup.IsValid()) return;

                    var uri = GetCollectionUri(managedCollectionLoadoutGroup.Collection);
                    serviceProvider.GetRequiredService<IOSInterop>().OpenUri(uri);
                }, configureAwait: false
            );

            CommandCopyRevisionUrl = IsCollectionUploaded.ToReactiveCommand<Unit>(async (_, cancellationToken) =>
                {
                    var managedCollectionLoadoutGroup = ManagedCollectionLoadoutGroup.Load(_connection.Db, collectionGroupId.Value);
                    if (!managedCollectionLoadoutGroup.IsValid()) return;

                    var uri = GetCollectionUri(managedCollectionLoadoutGroup.Collection);

                    // copy to clipboard instead of opening the URL directly
                    await _avaloniaInterop.SetClipboardTextAsync(uri.AbsoluteUri);
                }, configureAwait: false
            );

            CommandRenameGroup = new ReactiveCommand<Unit>(async (_, cancellationToken) =>
                {
                    var dialog = LoadoutDialogs.RenameCollection(CollectionName.Value);
                    var result = await windowManager.ShowDialog(dialog, DialogWindowType.Modal);
                    if (result.ButtonId != ButtonDefinitionId.Accept) return;

                    var newName = result.InputText;
                    if (string.IsNullOrWhiteSpace(newName)) return;

                    if (CollectionCreator.IsCollectionUploaded(_connection, collectionGroupId.Value, out var collection))
                    {
                        await _nexusModsLibrary.EditCollectionName(collection, newName, cancellationToken);
                    }

                    using var tx = _connection.BeginTransaction();
                    tx.Add(collectionGroupId.Value, LoadoutItem.Name, newName);
                    await tx.Commit();

                    CollectionName.Value = newName;
                    TabTitle = newName;
                }
            );

            var canDelete = CollectionDeleteHelpers.ObserveCanDeleteCollection(collectionGroupId.Value, _connection)
                .ToObservable();

            CommandDeleteGroup = canDelete.ToReactiveCommand<Unit>(async (_, _) =>
            {
                var confirmed = await CollectionDeleteHelpers.ShowDeleteConfirmationDialogAsync(collectionGroup.Name, WindowManager);
                if (confirmed)
                {
                    await CollectionDeleteHelpers.DeleteCollectionAsync(collectionGroupId.Value, _loadoutManager, GetWorkspaceController(), _connection, _notificationService);
                    CommandOpenLibraryPage?.Execute(NavigationInformation.From(OpenPageBehaviorType.ReplaceTab));
                }
            });
        }
        else
        {
            CollectionName = new BindableReactiveProperty<string>(value: string.Empty);
            IsCollectionUploaded = new BindableReactiveProperty<bool>(value: false);
            CollectionStatus = new BindableReactiveProperty<CollectionStatus>();
            RevisionStatus = new BindableReactiveProperty<RevisionStatus>();
            RevisionNumber = new BindableReactiveProperty<RevisionNumber>();
            LastUploadedDate = new BindableReactiveProperty<DateTimeOffset>();

            TabTitle = Language.LoadoutViewPageTitle;
            TabIcon = IconValues.FormatAlignJustify;
            RulesSectionViewModel = new SortingSelectionViewModel(serviceProvider, windowManager, loadoutId,
                Optional<Observable<bool>>.None
            );
            CommandRenameGroup = new ReactiveCommand();
            CommandShareCollection = new ReactiveCommand();
            CommandUploadDraftRevision = new ReactiveCommand();
            CommandUploadAndPublishRevision = new ReactiveCommand();
            CommandOpenRevisionUrl = new ReactiveCommand();
            CommandCopyRevisionUrl = new ReactiveCommand();
            CommandChangeVisibility = new ReactiveCommand();
            CommandDeleteGroup = new ReactiveCommand();
        }

        CommandDeselectItems = new ReactiveCommand<Unit>(_ => { Adapter.ClearSelection(); });

        var viewModFilesArgumentsSubject = new BehaviorSubject<Optional<LoadoutItemGroup.ReadOnly>>(Optional<LoadoutItemGroup.ReadOnly>.None);

        var loadout = Loadout.Load(_connection.Db, loadoutId);
        EmptyStateTitleText = string.Format(Language.LoadoutGridViewModel_EmptyModlistTitleString, loadout.InstallationInstance.Game.DisplayName);
        CommandOpenLibraryPage = new ReactiveCommand<NavigationInformation>(info =>
            {
                var pageData = new PageData
                {
                    FactoryId = LibraryPageFactory.StaticId,
                    Context = new LibraryPageContext
                    {
                        LoadoutId = loadoutId,
                    },
                };
                var workspaceController = GetWorkspaceController();
                var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
                workspaceController.OpenPage(workspaceController.ActiveWorkspaceId, pageData, behavior);
            }
        );

        HasRulesSection = true;

        SelectedSubTab = selectedSubTab switch
        {
            { HasValue: true, Value: LoadoutPageSubTabs.Rules } => HasRulesSection ? LoadoutPageSubTabs.Rules : LoadoutPageSubTabs.Mods,
            _ => LoadoutPageSubTabs.Mods,
        };

        CommandOpenFilesPage = viewModFilesArgumentsSubject
            .Select(viewModFilesArguments => viewModFilesArguments.HasValue)
            .ToReactiveCommand<NavigationInformation>(info =>
                {
                    var group = viewModFilesArgumentsSubject.Value;
                    if (!group.HasValue) return;
                    
                    OpenViewModFilesPage(group.Value, info, GetWorkspaceController(), _connection);
                },
                false
            );

        var hasValidRemoveSelection = Adapter.SelectedModels
            .ObserveChanged()
            .SelectMany(_ =>
                {
                    var observables = Adapter.SelectedModels.Select(model =>
                        model.GetObservable<LoadoutComponents.LockedEnabledState>(LoadoutColumns.EnabledState.LockedEnabledStateComponentKey)
                    );

                    return Observable.CombineLatest(observables)
                        // if all items are readonly, or list is empty, no valid selection
                        .Select(list => !list.All(x => x.HasValue));
                }
            );

        CommandRemoveItem = hasValidRemoveSelection
            .ToReactiveCommand<Unit>(async (_, _) =>
                {
                    var ids = Adapter.SelectedModels
                        .SelectMany(static itemModel => GetLoadoutItemIds(itemModel))
                        .ToHashSet()
                        .Where(id => !IsRequired(id, _connection))
                        .Select(x => (LoadoutItemGroupId)x.Value)
                        .ToArray();

                    if (ids.Length == 0) return;
                    
                    var result = await ShowUninstallModsConformationDialog(ids, windowManager, _connection);
                    if (result.ButtonId != ButtonDefinitionId.Accept) return;

                    await _loadoutManager.RemoveItems(ids);
                    _notificationService.ShowToast(Language.ToastNotification_Mods_removed);
                },
                awaitOperation: AwaitOperation.Sequential,
                initialCanExecute: false,
                configureAwait: false
            );

        this.WhenActivated(disposables =>
            {
                Adapter.Activate().AddTo(disposables);

                Adapter.MessageSubject.SubscribeAwait(async (message, cancellationToken) =>
                    {
                        await message.Match<Task>(
                            toggleEnableStateMessage => 
                                HandleToggleItemEnabledState(toggleEnableStateMessage.Ids, _connection),
                            openCollectionMessage =>
                            {
                                HandleOpenItemCollectionPage(openCollectionMessage.Ids, 
                                    openCollectionMessage.NavigationInformation, 
                                    loadoutId, GetWorkspaceController(), _connection);
                                return Task.CompletedTask;
                            },
                            viewModPageMessage =>
                            {
                                HandleOpenModPageFor(viewModPageMessage.Ids, _connection, 
                                    _serviceProvider.GetRequiredService<IOSInterop>(), 
                                    cancellationToken);
                                return Task.CompletedTask;
                            },
                            viewModFilesMessage =>
                            {
                                HandleViewModFiles(viewModFilesMessage.Ids, 
                                    viewModFilesMessage.NavigationInformation, 
                                    _connection, GetWorkspaceController());
                                return Task.CompletedTask;
                            },
                            uninstallItemMessage => HandleUninstallItem(uninstallItemMessage.Ids, windowManager, _connection)
                        );
                    }, awaitOperation: AwaitOperation.Parallel, configureAwait: false
                ).AddTo(disposables);

                // Update the selection count based on the selected models
                Adapter.SelectedModels
                    .ObserveChanged()
                    .Select(Adapter, static (_, adapter) => adapter.SelectedModels
                        .SelectMany(GetLoadoutItemIds)
                        .Distinct()
                        .Count()
                    )
                    .ObserveOnUIThreadDispatcher()
                    .Subscribe(this, (count, self) => self.SelectionCount.Value = count);

                // Compute the target group for the ViewFilesCommand
                Adapter.SelectedModels
                    .ObserveCountChanged(notifyCurrentCount: true)
                    .Select(this, static (count, vm) => count == 1 ? vm.Adapter.SelectedModels.First() : null)
                    .ObserveOnThreadPool()
                    .Select(_connection, static (model, connection) =>
                        {
                            if (model is null) return Optional<LoadoutItemGroup.ReadOnly>.None;
                            return LoadoutGroupFilesViewModel.GetViewModFilesLoadoutItemGroup(GetLoadoutItemIds(model).ToArray(), connection);
                        }
                    )
                    .ObserveOnUIThreadDispatcher()
                    .Subscribe(viewModFilesArgumentsSubject.OnNext)
                    .AddTo(disposables);

                if (collectionGroupId.HasValue)
                {
                    IsCollectionUploaded
                        .Where(isUploaded => isUploaded)
                        .ObserveOnThreadPool()
                        .Select((_connection, collectionGroupId.Value), static (_, state) => ManagedCollectionLoadoutGroup.Load(state._connection.Db, state.Value))
                        .SubscribeAwait(this, static (group, self, cancellationToken) => self.UpdateCollectionInfo(group, cancellationToken))
                        .AddTo(disposables);

                    _connection
                        .ObserveDatoms(collectionGroupId.Value.Value)
                        .QueryWhenChanged(_ => new ManagedCollectionLoadoutGroup.ReadOnly(_connection.Db, collectionGroupId.Value))
                        .ToObservable()
                        .Where(model => model.IsValid())
                        .Subscribe(this, (model, self) =>
                            {
                                self.CollectionStatus.Value = model.Collection.Status.ValueOr(() => Abstractions.NexusModsLibrary.Models.CollectionStatus.Unlisted);
                                self.RevisionStatus.Value = model.ToStatus();
                                self.RevisionNumber.Value = model.CurrentRevisionNumber;
                                self.LastUploadedDate.Value = model.LastUploadDate;
                            }
                        )
                        .AddTo(disposables);

                    // NOTE(erri120): This can be improved. We don't have an easy way of knowing whether a group
                    // or any of the children changed. We have that for Loadouts but not for LoadoutGroups.
                    // This query will produce false positives but not false negatives, the latter being more
                    // important.
                    LoadoutQueries2
                        .RevisionsWithChildUpdates(_connection, loadout)
                        .Subscribe(_ => HasOutstandingChanges.Value = true)
                        .AddTo(disposables);
                }

                LoadoutDataProviderHelper.CountAllLoadoutItems(serviceProvider, loadoutFilter)
                    .OnUI()
                    .Subscribe(count => ItemCount.Value = count)
                    .DisposeWith(disposables);
            }
        );
    }

    private static async Task<StandardDialogResult> ShowUninstallModsConformationDialog(LoadoutItemGroupId[] ids, IWindowManager windowManager, IConnection connection)
    {
        // Comma-separated list of parent collection names
        var parentCollectionNames = string.Join(",\n",
            ids
                .Select(id => LoadoutItem.Load(connection.Db, id))
                .Where(item => item.IsValid() && item.HasParent())
                .Select(item => item.Parent.AsLoadoutItem().Name)
                .Distinct()
                .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
        );

        var dialog = DialogFactory.CreateStandardDialog(
            title: "Uninstall mod(s)",
            new StandardDialogParameters()
            {
                Text = $"""
                         This will remove the selected mod(s) from:
                         
                         {parentCollectionNames}
                         
                         ✓ The mod(s) will stay in your Library
                         ✓ You can reinstall anytime
                         """,
            },
            buttonDefinitions:
            [
                DialogStandardButtons.Cancel,
                new DialogButtonDefinition("Uninstall",
                    ButtonDefinitionId.Accept,
                    ButtonAction.Accept,
                    ButtonStyling.Default
                )
            ]
        );

        var result = await windowManager.ShowDialog(dialog, DialogWindowType.Modal);
        return result;
    }

    internal static void HandleOpenModPageFor(LoadoutItemId[] ids, IConnection connection, IOSInterop os, CancellationToken cancellationToken)
    {
        if (ids.Length == 0) return;
        var loadoutItemId = ids.First();
        
        LibraryLinkedLoadoutItem.TryGet(connection.Db, loadoutItemId.Value, out var linkedItem);
        if (linkedItem is null) return;
        var libraryItem = linkedItem.Value.LibraryItemId;
        NexusModsLibraryItem.TryGet(connection.Db, libraryItem.Value, out var nexusModsLibraryItem);
        if (nexusModsLibraryItem is null) return;
        var modPage = nexusModsLibraryItem.Value.ModPageMetadata;

        var url = NexusModsUrlBuilder.GetModUri(modPage.GameDomain, modPage.Uid.ModId);
        os.OpenUri(url);
    }

    private Uri GetCollectionChangelogUri(CollectionMetadata.ReadOnly collection)
    {
        var mappingCache = _serviceProvider.GetRequiredService<IGameDomainToGameIdMappingCache>();
        var gameDomain = mappingCache[collection.GameId];
        var uri = NexusModsUrlBuilder.GetCollectionChangelogUri(gameDomain, collection.Slug, revisionNumber: new Optional<RevisionNumber>());
        return uri;
    }

    private Uri GetCollectionUri(CollectionMetadata.ReadOnly collection)
    {
        var mappingCache = _serviceProvider.GetRequiredService<IGameDomainToGameIdMappingCache>();
        var gameDomain = mappingCache[collection.GameId];
        var uri = NexusModsUrlBuilder.GetCollectionUri(gameDomain, collection.Slug, revisionNumber: new Optional<RevisionNumber>());
        return uri;
    }

    internal static async Task HandleToggleItemEnabledState(LoadoutItemId[] ids, IConnection connection)
    {
        var toggleableItems = ids
            .Select(loadoutItemId => LoadoutItem.Load(connection.Db, loadoutItemId))
            // Exclude collection required items
            .Where(item => !IsRequired(item.Id, connection))
            // Exclude items that are part of a collection that is disabled
            .Where(item => !(item.Parent.TryGetAsCollectionGroup(out var collectionGroup)
                             && collectionGroup.AsLoadoutItemGroup().AsLoadoutItem().IsDisabled)
            )
            .ToArray();

        if (toggleableItems.Length == 0) return;

        // We only enable if all items are disabled, otherwise we disable
        var shouldEnable = toggleableItems.All(loadoutItem => loadoutItem.IsDisabled);

        using var tx = connection.BeginTransaction();

        foreach (var id in toggleableItems)
        {
            if (shouldEnable)
            {
                tx.Retract(id, LoadoutItem.Disabled, Null.Instance);
            }
            else
            {
                tx.Add(id, LoadoutItem.Disabled, Null.Instance);
            }
        }

        await tx.Commit();
    }

    internal static void HandleOpenItemCollectionPage(
        LoadoutItemId[] ids,
        NavigationInformation navInfo,
        LoadoutId loadoutId,
        IWorkspaceController workspaceController,
        IConnection connection)
    {
        if (ids.Length == 0) return;

        // Open the collection page for the first item
        var firstItemId = ids.First();

        var parentGroup = LoadoutItem.Load(connection.Db, firstItemId).Parent;
        if (!parentGroup.TryGetAsCollectionGroup(out var collectionGroup)) return;

        if (collectionGroup.TryGetAsNexusCollectionLoadoutGroup(out var nexusCollectionGroup))
        {
            var nexusCollPageData = new PageData
            {
                FactoryId = CollectionLoadoutPageFactory.StaticId,
                Context = new CollectionLoadoutPageContext
                {
                    LoadoutId = loadoutId,
                    GroupId = nexusCollectionGroup.Id,
                },
            };
            var nexusPageBehavior = workspaceController.GetOpenPageBehavior(nexusCollPageData, navInfo);
            workspaceController.OpenPage(workspaceController.ActiveWorkspaceId, nexusCollPageData, nexusPageBehavior);

            return;
        }

        var pageData = new PageData
        {
            FactoryId = LoadoutPageFactory.StaticId,
            Context = new LoadoutPageContext
            {
                LoadoutId = loadoutId,
                GroupScope = collectionGroup.CollectionGroupId,
            },
        };

        var behavior = workspaceController.GetOpenPageBehavior(pageData, navInfo);
        workspaceController.OpenPage(workspaceController.ActiveWorkspaceId, pageData, behavior);
    }
    
    public static void HandleViewModFiles(LoadoutItemId[] ids, NavigationInformation navInfo, IConnection connection, IWorkspaceController workspaceController)
    {
        if (ids.Length == 0) return;
        var loadoutItemId = ids.First();
        
        var group = LoadoutItemGroup.Load(connection.Db, loadoutItemId);

        OpenViewModFilesPage(group, navInfo, workspaceController, connection);
    }

    private static void OpenViewModFilesPage(LoadoutItemGroup.ReadOnly group, NavigationInformation navInfo, IWorkspaceController workspaceController, IConnection connection)
    {
        var isReadonly = group.AsLoadoutItem()
            .GetThisAndParents()
            .Any(item => IsRequired(item.LoadoutItemId, connection));

        var pageData = new PageData
        {
            FactoryId = LoadoutGroupFilesPageFactory.StaticId,
            Context = new LoadoutGroupFilesPageContext
            {
                GroupIds = [group.Id],
                IsReadOnly = isReadonly,
            },
        };
       
        var behavior = workspaceController.GetOpenPageBehavior(pageData, navInfo);
        workspaceController.OpenPage(workspaceController.ActiveWorkspaceId, pageData, behavior);
    }

    public static async Task HandleUninstallItem(LoadoutItemId[] ids, IWindowManager windowManager, IConnection connection)
    {
        if (ids.Length == 0) return;
        
        var removableIds = ids
            .ToHashSet()
            .Where(id => !IsRequired(id, connection))
            .Select(x => (LoadoutItemGroupId)x.Value)
            .ToArray();
        
        if (removableIds.Length == 0) return;
        
        var result = await ShowUninstallModsConformationDialog(removableIds, windowManager, connection);

        if (result.ButtonId != ButtonDefinitionId.Accept) return;
        

        using var tx = connection.BeginTransaction();
        
        foreach (var itemId in removableIds)
            tx.Delete(itemId, recursive: true);
        
        await tx.Commit();
    }

    private async ValueTask UpdateCollectionInfo(ManagedCollectionLoadoutGroup.ReadOnly managedCollectionLoadoutGroup, CancellationToken cancellationToken)
    {
        var graphQlResult = await _nexusModsLibrary.GetLastPublishedRevisionNumber(managedCollectionLoadoutGroup.Collection, cancellationToken);

        // TODO: handle errors
        var lastPublishedRevisionNumber = graphQlResult.AssertHasData();

        using var tx = _connection.BeginTransaction();
        if (lastPublishedRevisionNumber.HasValue)
        {
            tx.Add(managedCollectionLoadoutGroup, ManagedCollectionLoadoutGroup.LastPublishedRevisionNumber, lastPublishedRevisionNumber.Value);
        }
    }

    private static IEnumerable<LoadoutItemId> GetLoadoutItemIds(CompositeItemModel<EntityId> itemModel)
    {
        return itemModel.Get<LoadoutComponents.LoadoutItemIds>(LoadoutColumns.EnabledState.LoadoutItemIdsComponentKey).ItemIds;
    }

    private static bool IsRequired(LoadoutItemId id, IConnection connection)
    {
        return NexusCollectionItemLoadoutGroup.IsRequired.TryGetValue(LoadoutItem.Load(connection.Db, id), out var isRequired) && isRequired;
    }

    // private static IClipboard GetClipboard()
    // {
    //     if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
    //     {
    //         return window.Clipboard!;
    //     }
    //
    //     return null!;
    // }
}
