using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Collections;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.CollectionDeleteService;

/// <summary>
/// Service for managing collection deletion operations.
/// </summary>
public interface ICollectionDeleteService
{
    /// <summary>
    /// Gets the action text for the collection delete operation.
    /// </summary>
    /// <param name="collectionId">The collection group identifier.</param>
    /// <returns>The action text to display.</returns>
    string GetActionText(CollectionGroupId collectionId);
    
    /// <summary>
    /// Determines whether the collection can be deleted.
    /// </summary>
    /// <param name="collectionId">The collection group identifier.</param>
    /// <returns>True if the collection can be deleted; otherwise, false.</returns>
    bool CanDeleteCollection(CollectionGroupId collectionId);
    
    /// <summary>
    /// Observes whether the collection can be deleted.
    /// </summary>
    /// <param name="collectionId">The collection group identifier.</param>
    /// <returns>An observable that indicates whether the collection can be deleted.</returns>
    IObservable<bool> ObserveCanDeleteCollection(CollectionGroupId collectionId);
    
    /// <summary>
    /// Deletes the collection asynchronously.
    /// </summary>
    /// <param name="collectionId">The collection group identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteCollectionAsync(CollectionGroupId collectionId);
    
    /// <summary>
    /// Shows a delete confirmation dialog for the collection.
    /// </summary>
    /// <param name="collectionId">The collection group identifier.</param>
    /// <param name="windowManager">The window manager to show the dialog.</param>
    /// <returns>True if the user confirms deletion; otherwise, false.</returns>
    Task<bool> ShowDeleteConfirmationDialogAsync(CollectionGroupId collectionId, IWindowManager windowManager);
    
    /// <summary>
    /// Deletes the Nexus collection asynchronously.
    /// </summary>
    /// <param name="nexusCollectionGroup">The Nexus collection loadout group.</param>
    /// <param name="workspaceController">The workspace controller for navigation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteNexusCollectionAsync(NexusCollectionLoadoutGroup.ReadOnly nexusCollectionGroup, IWorkspaceController workspaceController);
}
