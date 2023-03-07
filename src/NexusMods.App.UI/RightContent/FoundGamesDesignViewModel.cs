using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.ViewModels;

namespace NexusMods.App.UI.RightContent;

public class FoundGamesDesignViewModel : AViewModel<IFoundGamesViewModel>, IFoundGamesViewModel
{
    private readonly IEnumerable<GameWidgetDesignViewModel> _games;

    public FoundGamesDesignViewModel()
    {
        var games = Enumerable.Range(0, 10)
            .Select(g => new GameWidgetDesignViewModel());

        Games = new ReadOnlyObservableCollection<IGameWidgetViewModel>(new ObservableCollection<IGameWidgetViewModel>(games));
    }

    public ReadOnlyObservableCollection<IGameWidgetViewModel> Games { get; }
}
