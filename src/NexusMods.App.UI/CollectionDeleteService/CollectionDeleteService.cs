using Avalonia.Threading;
using DynamicData;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Collections;
using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Pages.CollectionDownload;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Collections;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Dialog;
using NexusMods.UI.Sdk.Dialog.Enums;
using R3;

namespace NexusMods.App.UI.CollectionDeleteService;

public class CollectionDeleteService(
    IConnection connection,
    IWindowNotificationService notificationService) : ICollectionDeleteService
{
    
    /// <inheritdoc />
    public bool CanDeleteCollection(CollectionGroupId collectionId)
    {
        var group = CollectionGroup.Load(connection.Db, collectionId);
        if (group.IsReadOnly)
            return false;

        var loadoutId = group.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId;
        var collectionCount = CollectionGroup
            .All(connection.Db)
            .Count(g => g.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == loadoutId && !g.IsReadOnly);

        return collectionCount > 1;
    }

    /// <inheritdoc />
    public IObservable<bool> ObserveCanDeleteCollection(CollectionGroupId collectionId)
    {
        var group = CollectionGroup.Load(connection.Db, collectionId);
        var loadoutId = group.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId;

        return CollectionGroup
            .ObserveAll(connection)
            .FilterImmutable(g => g.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == loadoutId && !g.IsReadOnly)
            .QueryWhenChanged(query => query.Count)
            .ToObservable()
            .Select(count => count > 1)
            .ObserveOnUIThreadDispatcher()
            .AsSystemObservable();
    }

    /// <inheritdoc />
    public async Task DeleteCollectionAsync(CollectionGroupId collectionId)
    {
        await CollectionCreator.DeleteCollectionGroup(connection, collectionId, CancellationToken.None);
        notificationService.ShowToast(Language.ToastNotification_Collection_removed);
    }

    /// <inheritdoc />
    public async Task<bool> ShowDeleteConfirmationDialogAsync(CollectionGroupId collectionId, IWindowManager windowManager)
    {
        var group = CollectionGroup.Load(connection.Db, collectionId);
        var collectionName = group.AsLoadoutItemGroup().AsLoadoutItem().Name;

        var dialog = DialogFactory.CreateStandardDialog(
            title: Language.Loadout_DeleteCollection_Confirmation_Title,
            new StandardDialogParameters()
            {
                Text = string.Format(Language.Loadout_DeleteCollection_Confirmation_Message, collectionName),
            },
            buttonDefinitions:
            [
                DialogStandardButtons.Cancel,
                new DialogButtonDefinition(Language.Loadout_DeleteCollection_Confirmation_Delete,
                    ButtonDefinitionId.Accept,
                    ButtonAction.Accept,
                    ButtonStyling.Destructive
                )
            ]
        );

        var result = await windowManager.ShowDialog(dialog, DialogWindowType.Modal);
        return result.ButtonId == ButtonDefinitionId.Accept;
    }

    /// <inheritdoc />
    public async Task DeleteNexusCollectionAsync(NexusCollectionLoadoutGroup.ReadOnly nexusCollectionGroup, IWorkspaceController workspaceController)
    {
        var group = nexusCollectionGroup.AsCollectionGroup();
        
        // Switch away from this page since its collection will be deleted
        var pageData = new PageData
        {
            FactoryId = CollectionDownloadPageFactory.StaticId,
            Context = new CollectionDownloadPageContext()
            {
                TargetLoadout = group.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId,
                CollectionRevisionMetadataId = nexusCollectionGroup.RevisionId,
            },
        };

        await Dispatcher.UIThread.InvokeAsync(() =>
            workspaceController.ReplacePages<CollectionLoadoutPageContext>(
                context => context.GroupId == CollectionGroupId.From(group.Id), pageData)
        );
        
        using var tx = connection.BeginTransaction();
        
        // Delete collection loadout group and all installed mods inside it
        tx.Delete(nexusCollectionGroup.Id, recursive: true);
        
        await tx.Commit();
        
        notificationService.ShowToast(Language.ToastNotification_Collection_removed);
    }
}
