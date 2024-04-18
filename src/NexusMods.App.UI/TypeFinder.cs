using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Pages.Diagnostics;
using NexusMods.App.UI.Pages.Diff.ApplyDiff;
using NexusMods.App.UI.Pages.Downloads;
using NexusMods.App.UI.Pages.LoadoutGrid;
using NexusMods.App.UI.Pages.ModInfo;
using NexusMods.App.UI.Pages.MyGames;
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
        typeof(DummyPageContext),
        typeof(NewTabPageContext),
        typeof(LoadoutGridContext),
        typeof(InProgressPageContext),
        typeof(MyGamesPageContext),
        typeof(ModInfoPageContext),
        typeof(DiagnosticListPageContext),
        typeof(ApplyDiffPageContext),

        // workspace context
        typeof(EmptyContext),
        typeof(HomeContext),
        typeof(LoadoutContext),
        typeof(DownloadsContext),

        // other
        typeof(WindowData),
    };
}
