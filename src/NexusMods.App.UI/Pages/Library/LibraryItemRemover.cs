using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.LibraryDeleteConfirmation;
using NexusMods.MnemonicDB.Abstractions;
namespace NexusMods.App.UI.Pages.Library;

/// <summary>
///     Utility helper class for removing a set of library items from inside a ViewModel.
/// </summary>
/// <remarks>
///     This is here to help easier migration to new LoadoutItems based library UI
///     when the time comes.
/// </remarks>
public static class LibraryItemRemover
{
    public static async Task RemoveAsync(IConnection conn, IOverlayController overlayController, ILibraryService libraryService, LibraryItem.ReadOnly[] toRemove)
    {
        var warnings = LibraryItemDeleteWarningDetector.Process(conn, toRemove);
        var alphaWarningViewModel = LibraryItemDeleteConfirmationViewModel.FromWarningDetector(warnings);
        var controller = overlayController;
        alphaWarningViewModel.Controller = controller;
        var result = await controller.EnqueueAndWait(alphaWarningViewModel);
        
        if (result)
            await libraryService.RemoveItems(toRemove);
    }
}
