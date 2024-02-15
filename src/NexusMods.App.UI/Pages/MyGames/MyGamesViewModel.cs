using System.Collections;
using JetBrains.Annotations;
using NexusMods.Abstractions.Games;
using NexusMods.App.UI.Controls.FoundGames;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.MyGames;

[UsedImplicitly]
public class MyGamesViewModel : APageViewModel<IMyGamesViewModel>, IMyGamesViewModel
{
    [Reactive] public IFoundGamesViewModel FoundGames { get; set; }

    [Reactive] public IFoundGamesViewModel AllGames { get; set; }

    public MyGamesViewModel(
        IWindowManager windowManager,
        IFoundGamesViewModel foundGames,
        IFoundGamesViewModel allGames,
        IEnumerable<IGame> games) : base(windowManager)
    {
        var gamesList = games.ToList();
        FoundGames = foundGames;
        foundGames.InitializeFromFound(gamesList);

        AllGames = allGames;
        allGames.InitializeManual(gamesList);

        this.WhenActivated(() =>
        {
            GetWorkspaceController().SetTabTitle(Language.MyGames, WorkspaceId, PanelId, TabId);

            return Array.Empty<IDisposable>();
        });
    }
}
