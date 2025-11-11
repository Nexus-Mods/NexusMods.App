using NexusMods.Abstractions.Library;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.LibraryDeleteConfirmation;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Models.Library;

namespace NexusMods.App.UI.Pages.Library;

/// <summary>
///     Utility helper class for removing a set of library items from inside a ViewModel.
/// </summary>
/// <remarks>
///     This wraps the actual removal from loadouts and user facing UI confirmation
///     into a single operation, so you can call this from anywhere and not have to
///     worry too much about it.
/// </remarks>
public static class LibraryItemRemover
{
    public static async Task RemoveAsync(
        IConnection conn,
        IOverlayController overlayController,
        ILibraryService libraryService,
        LibraryItem.ReadOnly[] toRemove)
    {
        var warnings = LibraryItemDeleteWarningDetector.Process(conn, toRemove);
        var alphaWarningViewModel = LibraryItemDeleteConfirmationViewModel.FromWarningDetector(warnings);
        alphaWarningViewModel.Controller = overlayController;
        var result = await overlayController.EnqueueAndWait(alphaWarningViewModel);
        if (result)
            await libraryService.RemoveLibraryItems(toRemove);
    }
}
