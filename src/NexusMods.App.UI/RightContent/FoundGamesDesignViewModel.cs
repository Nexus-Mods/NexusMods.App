using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.DataModel.Games;

namespace NexusMods.App.UI.RightContent;

public class FoundGamesDesignViewModel : AViewModel<IFoundGamesViewModel>, IFoundGamesViewModel
{

    public FoundGamesDesignViewModel()
    {
        var games = Enumerable.Range(0, 10)
            .Select(_ => new GameWidgetDesignViewModel());

        Games = new ReadOnlyObservableCollection<IGameWidgetViewModel>(new ObservableCollection<IGameWidgetViewModel>(games));
    }

    public ReadOnlyObservableCollection<IGameWidgetViewModel> Games { get; }
    public void InitializeFromFound(IEnumerable<IGame> games)
    {
        throw new NotImplementedException();
    }

    public void InitializeManual(IEnumerable<IGame> games)
    {
        throw new NotImplementedException();
    }
}
