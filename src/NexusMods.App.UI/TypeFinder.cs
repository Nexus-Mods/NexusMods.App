using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.App.UI.Pages.Changelog;
using NexusMods.App.UI.Pages.CollectionDownload;
using NexusMods.App.UI.Pages.Diagnostics;
using NexusMods.App.UI.Pages.Diff.ApplyDiff;
using NexusMods.App.UI.Pages.Downloads;
using NexusMods.App.UI.Pages.ItemContentsFileTree;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.App.UI.Pages.MyGames;
using NexusMods.App.UI.Pages.MyLoadouts;
using NexusMods.App.UI.Pages.Settings;
using NexusMods.App.UI.Pages.Sorting;
using NexusMods.App.UI.Pages.TextEdit;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI;

internal class TypeFinder : ITypeFinder
{
    public IEnumerable<Type> DescendentsOf(Type type)
    {
        return AllTypes.Where(t => t.IsAssignableTo(type));
    }

    private static IEnumerable<Type> AllTypes => new[]
    {
        // factory context
        typeof(MyGamesPageContext),
        typeof(DiagnosticListPageContext),
        typeof(ApplyDiffPageContext),
        typeof(SettingsPageContext),
        typeof(ChangelogPageContext),
        typeof(TextEditorPageContext),
        typeof(MyLoadoutsPageContext),
        typeof(ItemContentsFileTreePageContext),
        typeof(LibraryPageContext),
        typeof(LoadoutPageContext),
        typeof(LoadOrdersWIPPageContext),
        typeof(CollectionLoadoutPageContext),

        // workspace context
        typeof(EmptyContext),
        typeof(HomeContext),
        typeof(LoadoutContext),
        typeof(DownloadsContext),
        typeof(CollectionDownloadPageContext),

        // other
        typeof(WindowData),
    };
}
