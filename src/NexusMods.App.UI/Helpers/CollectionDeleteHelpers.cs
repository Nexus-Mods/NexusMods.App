using Avalonia.Threading;
using DynamicData;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.Pages.CollectionDownload;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Collections;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Dialog;
using NexusMods.UI.Sdk.Dialog.Enums;
using R3;

namespace NexusMods.App.UI.Helpers;

public static class CollectionDeleteHelpers
{
    
    /// <summary>
    /// Determines whether the collection group can be deleted.
    /// </summary>
    /// <param name="collectionId">The collection group identifier.</param>
    /// <returns>True if the collection can be deleted; otherwise, false.</returns>
    public static bool CanDelete(CollectionGroupId collectionId, IConnection connection)
    {
        var group = CollectionGroup.Load(connection.Db, collectionId);
        if (group.IsReadOnly)
            return true;

        var loadoutId = group.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId;
        var collectionCount = CollectionGroup
            .All(connection.Db)
            .Count(g => g.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == loadoutId && !g.IsReadOnly);

        return collectionCount > 1;
    }

    /// <summary>
    /// Observes whether the collection group can be deleted.
    /// </summary>
    /// <remarks>
    /// Editable collections can only be deleted if there is more than one editable collection.
    /// Nexus collections (read-only) can always be deleted.
    /// </remarks>
    /// <param name="collectionId">The collection group identifier.</param>
    /// <returns>An observable that indicates whether the collection can be deleted.</returns>
    public static IObservable<bool> ObserveCanDeleteCollection(CollectionGroupId collectionId, IConnection connection)
    {
        var group = CollectionGroup.Load(connection.Db, collectionId);
        // Read-only collections can always be removed
        if (group.IsReadOnly)
            return System.Reactive.Linq.Observable.Return(true);
        
        var loadoutId = group.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId;
        
        // Editable collection can only be deleted if they are not the last one
        return CollectionGroup
            .ObserveAll(connection)
            .FilterImmutable(g => g.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == loadoutId && !g.IsReadOnly)
            .QueryWhenChanged(query => query.Count)
            .ToObservable()
            .Select(count => count > 1)
            .ObserveOnUIThreadDispatcher()
            .AsSystemObservable();
    }

    /// <summary>
    /// Deletes the collection group asynchronously.
    /// </summary>
    /// <remarks>
    /// This takes care of replacing any open pages related to the collection before deletion,
    /// as well as showing a toast notification after deletion.
    /// </remarks>
    /// <param name="collectionId">The collection group identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task DeleteCollectionAsync(
        CollectionGroupId collectionId, 
        IWorkspaceController workspaceController,
        IConnection connection, 
        IWindowNotificationService notificationService)
    {
        var group = CollectionGroup.Load(connection.Db, collectionId);
        
        if (group.TryGetAsNexusCollectionLoadoutGroup(out var nexusCollectionGroup))
        {
            // Replace installed nexus collection pages with the collection download pages
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
        } 
        else
        {
            // Replace editable collection pages with new tab pages
            await Dispatcher.UIThread.InvokeAsync(() =>
                workspaceController.ReplacePages<LoadoutPageContext>(
                    context => context.GroupScope == CollectionGroupId.From(group.Id))
            );
        }
        
        await CollectionCreator.DeleteCollectionGroup(connection, collectionId, CancellationToken.None);
        
        notificationService.ShowToast(Language.ToastNotification_Collection_removed);
    }

    /// <summary>
    /// Shows a delete confirmation dialog for the collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection to be deleted.</param>
    /// <param name="windowManager">The window manager to show the dialog.</param>
    /// <returns>True if the user confirms deletion; otherwise, false.</returns>
    public static async Task<bool> ShowDeleteConfirmationDialogAsync(
        string collectionName, 
        IWindowManager windowManager,
        IConnection connection)
    {
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
}
