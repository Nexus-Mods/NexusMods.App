using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.MyGames;

public class MyGamesDesignViewModel : APageViewModel<IMyGamesViewModel>, IMyGamesViewModel
{
    public ReadOnlyObservableCollection<IGameWidgetViewModel> ManagedGames { get; }
    public ReadOnlyObservableCollection<IGameWidgetViewModel> DetectedGames { get; }

    public MyGamesDesignViewModel() : base(new DesignWindowManager())
    {
        var games = Enumerable.Range(0, 10)
            .Select(_ => new GameWidgetDesignViewModel())
            .ToArray();

        ManagedGames = new ReadOnlyObservableCollection<IGameWidgetViewModel>(new ObservableCollection<IGameWidgetViewModel>(games));
        DetectedGames = new ReadOnlyObservableCollection<IGameWidgetViewModel>(new ObservableCollection<IGameWidgetViewModel>(games));
    }
}
