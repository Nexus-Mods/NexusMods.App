using Avalonia.Media.Imaging;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Jobs;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.CollectionDownload;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveCommand = R3.ReactiveCommand;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public class CollectionLoadoutViewModel : APageViewModel<ICollectionLoadoutViewModel>, ICollectionLoadoutViewModel
{
    public LoadoutTreeDataGridAdapter Adapter { get; }

    public CollectionLoadoutViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        CollectionLoadoutPageContext pageContext) : base(windowManager)
    {
        var connection = serviceProvider.GetRequiredService<IConnection>();

        var tilePipeline = ImagePipelines.GetCollectionTileImagePipeline(serviceProvider);
        var backgroundPipeline = ImagePipelines.GetCollectionBackgroundImagePipeline(serviceProvider);
        var userAvatarPipeline = ImagePipelines.GetUserAvatarPipeline(serviceProvider);
        
        var nexusCollectionGroup = NexusCollectionLoadoutGroup.Load(connection.Db, pageContext.GroupId);
        var group = nexusCollectionGroup.AsCollectionGroup();
        TabIcon = IconValues.CollectionsOutline;
        TabTitle = group.AsLoadoutItemGroup().AsLoadoutItem().Name;

        IsCollectionEnabled = !group.AsLoadoutItemGroup().AsLoadoutItem().IsDisabled;
        IsReadOnly = group.IsReadOnly;

        var revisionMetadata = pageContext.RevisionId.HasValue
            ? CollectionRevisionMetadata.Load(connection.Db, pageContext.RevisionId.Value)
            : Optional<CollectionRevisionMetadata.ReadOnly>.None;

        if (revisionMetadata.HasValue)
        {
            Name = revisionMetadata.Value.Collection.Name;
            RevisionNumber = revisionMetadata.Value.RevisionNumber;
            AuthorName = revisionMetadata.Value.Collection.Author.Name;
            IsLocalCollection = false;
            
            EndorsementCount = revisionMetadata.Value.Collection.Endorsements.ValueOr(0ul);
            TotalDownloads = revisionMetadata.Value.Collection.TotalDownloads.ValueOr(0ul);
            TotalSize = revisionMetadata.Value.TotalSize.ValueOr(Size.Zero);
            OverallRating = Percent.CreateClamped(revisionMetadata.Value.OverallRating.ValueOr(0));
        }
        else
        {
            Name = TabTitle;
            RevisionNumber = RevisionNumber.From(1);
            AuthorName = string.Empty;
            IsLocalCollection = true;
        }

        var loadoutFilter = new LoadoutFilter
        {
            LoadoutId = pageContext.LoadoutId,
            CollectionGroupId = LoadoutItemGroupId.From(pageContext.GroupId),
        };

        Adapter = new LoadoutTreeDataGridAdapter(serviceProvider, loadoutFilter);

        CommandToggle = new ReactiveCommand(
            executeAsync: async (_, _) =>
            {
                using var tx = connection.BeginTransaction();

                var shouldEnable = !IsCollectionEnabled;
                if (shouldEnable)
                {
                    tx.Retract(pageContext.GroupId, LoadoutItem.Disabled, Null.Instance);
                } else
                {
                    tx.Add(pageContext.GroupId, LoadoutItem.Disabled, Null.Instance);
                }

                await tx.Commit();
            },
            awaitOperation: AwaitOperation.Drop,
            configureAwait: false
        );
        
        CommandDeleteCollection = new ReactiveCommand(
            executeAsync: async (_, _) =>
            {
                // Switch away from this page since its collection will be deleted
                var pageData = new PageData
                {
                    FactoryId = CollectionDownloadPageFactory.StaticId,
                    Context = new CollectionDownloadPageContext()
                    {
                        TargetLoadout = pageContext.LoadoutId,
                        CollectionRevisionMetadataId = nexusCollectionGroup.RevisionId,
                    },
                };

                var workspaceController = GetWorkspaceController();
                var behavior = new OpenPageBehavior.ReplaceTab(PanelId, TabId);
                workspaceController.OpenPage(WorkspaceId, pageData, behavior, checkOtherPanels: false);
                
                using var tx = connection.BeginTransaction();
                
                // Delete collection loadout group and all installed mods inside it
                tx.Delete(nexusCollectionGroup.Id, recursive: true);
                
                await tx.Commit();
            },
            awaitOperation: AwaitOperation.Drop,
            configureAwait: false
        );

        CommandViewCollectionDownloadPage = ReactiveUI.ReactiveCommand.Create<NavigationInformation, System.Reactive.Unit>
        (
            info =>
            {
                var pageData = new PageData
                {
                    FactoryId = CollectionDownloadPageFactory.StaticId,
                    Context = new CollectionDownloadPageContext()
                    {
                        TargetLoadout = pageContext.LoadoutId,
                        CollectionRevisionMetadataId = nexusCollectionGroup.RevisionId,
                    },
                };

                var workspaceController = GetWorkspaceController();
                var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
                workspaceController.OpenPage(WorkspaceId, pageData, behavior);
                
                return System.Reactive.Unit.Default;
            }
        );

        CommandMakeLocalEditableCopy = new ReactiveCommand(
            executeAsync: async (_, _) =>
            {
                var dialog = DialogFactory.CreateStandardDialog(
                    "Collection Name",
                    new StandardDialogParameters()
                    {
                        Text = "This is the name of the new cloned collection.",
                        InputLabel = "Collection name",
                        InputWatermark = "(Local) " + group.AsLoadoutItemGroup().AsLoadoutItem().Name,
                        InputText = "(Local) " + group.AsLoadoutItemGroup().AsLoadoutItem().Name,
                    },
                    [
                        DialogStandardButtons.Cancel,
                        new DialogButtonDefinition(
                            "Create",
                            ButtonDefinitionId.Accept,
                            ButtonAction.Accept,
                            ButtonStyling.Primary
                        ),
                    ]
                );
                var result = await WindowManager.ShowDialog(dialog, DialogWindowType.Modal);
                if (result.ButtonId != ButtonDefinitionId.Accept || string.IsNullOrWhiteSpace(result.InputText))
                    return;
                
                var cloneId = await NexusCollectionLoadoutGroup.MakeEditableLocalCollection(group.Db.Connection, group.Id, result.InputText);
                
                var pageData = new PageData
                {
                    FactoryId = CollectionLoadoutPageFactory.StaticId,
                    Context = new CollectionLoadoutPageContext()
                    {
                        LoadoutId = group.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId, 
                        GroupId = CollectionGroupId.From(cloneId),
                    },
                };

                var workspaceController = GetWorkspaceController();
                var behavior = workspaceController.GetOpenPageBehavior(pageData, NavigationInformation.From(OpenPageBehaviorType.ReplaceTab));
                workspaceController.OpenPage(WorkspaceId, pageData, behavior);
            }
        );


        this.WhenActivated(disposables =>
        {
            Adapter.Activate().AddTo(disposables);
            
            connection.ObserveDatoms(LoadoutItem.ParentId, pageContext.GroupId)
                .QueryWhenChanged(datoms => datoms.Count)
                .OnUI()
                .Subscribe(count => InstalledModsCount = count)
                .AddTo(disposables);
            
            LoadoutItem
                .Observe(connection, pageContext.GroupId)
                .Select(static item => !item.IsDisabled)
                .OnUI()
                .Subscribe(isEnabled => IsCollectionEnabled = isEnabled)
                .AddTo(disposables);

            if (revisionMetadata.HasValue)
            {
                ImagePipelines
                    .CreateObservable(revisionMetadata.Value.CollectionId, tilePipeline)
                    .ObserveOnUIThreadDispatcher()
                    .Subscribe(this, static (image, self) => self.TileImage = image)
                    .AddTo(disposables);

                ImagePipelines
                    .CreateObservable(revisionMetadata.Value.CollectionId, backgroundPipeline)
                    .ObserveOnUIThreadDispatcher()
                    .Subscribe(this, static (image, self) => self.BackgroundImage = image)
                    .AddTo(disposables);

                ImagePipelines
                    .CreateObservable(revisionMetadata.Value.Collection.AuthorId, userAvatarPipeline)
                    .ObserveOnUIThreadDispatcher()
                    .Subscribe(this, static (image, self) => self.AuthorAvatar = image)
                    .AddTo(disposables);
            }

            Adapter.MessageSubject.SubscribeAwait(async (message, _) =>
            {
                // Toggle item state
                if (message.IsT0){
                    await LoadoutViewModel.ToggleItemEnabledState(message.AsT0.Ids, connection);
                    return;
                }

                // Open collection
                if (message.IsT1)
                {
                    var data = message.AsT1;
                    LoadoutViewModel.OpenItemCollectionPage(
                        data.Ids,
                        data.NavigationInformation,
                        pageContext.LoadoutId,
                        GetWorkspaceController(),
                        connection
                    );
                    return;
                }
                
            }, awaitOperation: AwaitOperation.Parallel, configureAwait: false).AddTo(disposables);
        });
    }

    public bool IsLocalCollection { get; }
    public bool IsReadOnly { get; }

    public string Name { get; }
    public ulong EndorsementCount { get; }
    public ulong TotalDownloads { get; }
    public Size TotalSize { get; }
    public Percent OverallRating { get; }

    public RevisionNumber RevisionNumber { get; }

    public string AuthorName { get; }

    [Reactive] public Bitmap? AuthorAvatar { get; private set; }

    [Reactive] public Bitmap? BackgroundImage { get; private set; }

    [Reactive] public Bitmap? TileImage { get; private set; }

    [Reactive] public bool IsCollectionEnabled { get; private set; }
    
    [Reactive] public int InstalledModsCount { get; private set; }
    
    public ReactiveCommand<Unit> CommandToggle { get; }
    public ReactiveCommand<Unit> CommandDeleteCollection { get; }
    
    public ReactiveCommand<Unit> CommandMakeLocalEditableCopy { get; }
    public ReactiveUI.ReactiveCommand<NavigationInformation, System.Reactive.Unit> CommandViewCollectionDownloadPage { get; }
}
