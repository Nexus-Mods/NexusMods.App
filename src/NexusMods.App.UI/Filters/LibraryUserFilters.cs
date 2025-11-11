using DynamicData;
using DynamicData.Alias;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Library;

namespace NexusMods.App.UI;

/// <summary>
/// Filters for displaying the contents of the Library to the user.
/// </summary>
[PublicAPI]
public static class LibraryUserFilters
{
    /// <summary>
    /// Returns whether the given library item should be shown to the user.
    /// </summary>
    public static bool ShouldShow(LibraryItem.ReadOnly libraryItem)
    {
        if (!libraryItem.TryGetAsLibraryFile(out var file))
            return false;

        return file.IsLocalFile() || file.IsDownloadedFile();
    }

    /// <summary>
    /// Returns an observable stream of all library items that should be shown
    /// to the user.
    /// </summary>
    public static IObservable<IChangeSet<LibraryItem.ReadOnly, EntityId>> ObserveFilteredLibraryItems(IConnection connection)
    {
        return LibraryItem.ObserveAll(connection).Where(ShouldShow);
    }
}
