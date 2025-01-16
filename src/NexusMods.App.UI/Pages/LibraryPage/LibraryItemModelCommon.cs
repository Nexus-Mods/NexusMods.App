namespace NexusMods.App.UI.Pages.LibraryPage;

/// <summary>
/// Common code between <see cref="ILibraryItemModel"/> implementations that aren't
/// necessarily directly tied to <see cref="ILibraryItemModel"/> (rows).
/// </summary>
public static class LibraryItemModelCommon
{
    /// <summary>
    /// Creates a formatted string to represent a version bump from
    /// <paramref name="oldVersion"/> to <paramref name="newVersion"/>
    /// </summary>
    public static string FormatModVersionUpdate(string oldVersion, string newVersion) => $"{oldVersion} → {newVersion}";
}
