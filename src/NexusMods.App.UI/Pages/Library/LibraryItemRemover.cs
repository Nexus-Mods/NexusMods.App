using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.LibraryDeleteConfirmation;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
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
    public static async Task RemoveAsync(
        IConnection conn,
        IOverlayController overlayController,
        ILibraryService libraryService,
        LibraryItem.ReadOnly[] toRemove,
        CancellationToken cancellationToken = default)
    {
        var warnings = LibraryItemDeleteWarningDetector.Process(conn, toRemove);
        var alphaWarningViewModel = LibraryItemDeleteConfirmationViewModel.FromWarningDetector(warnings);
        alphaWarningViewModel.Controller = overlayController;
        var result = await overlayController.EnqueueAndWait(alphaWarningViewModel);
        if (!result) return;

        if (result)
        {
            // Note(sewer) Can the person reviewing this code let me know their opinion of
            // whether this should be inlined into LibraryService or not?
            var loadouts = Loadout.All(conn.Db).ToArray();
            using var tx = conn.BeginTransaction();
            
            // Note. A loadout may technically still be updated in the background via the CLI,
            // However this is unlikelu, and the possibility of a concurrent update
            // is always possible, as long as we show a blocking dialog to the user.
            foreach (var itemInLoadout in warnings.ItemsInLoadouts)
            {
                foreach (var loadout in loadouts)
                {
                    foreach (var loadoutItem in loadout.GetLoadoutItemsByLibraryItem(itemInLoadout))
                        tx.Delete(loadoutItem, recursive: true);
                }
            }
            
            await tx.Commit();
            await libraryService.RemoveItems(toRemove);
        }
    }
}
