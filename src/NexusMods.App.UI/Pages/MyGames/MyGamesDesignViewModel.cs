using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Controls.MiniGameWidget;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.MyGames;

public class MyGamesDesignViewModel : APageViewModel<IMyGamesViewModel>, IMyGamesViewModel
{
    public ReadOnlyObservableCollection<IGameWidgetViewModel> DetectedGames { get; }
    public ReadOnlyObservableCollection<IMiniGameWidgetViewModel> SupportedGames { get; }

    public MyGamesDesignViewModel() : base(new DesignWindowManager())
    {
        var managedGames = Enumerable.Range(0, 1)
            .Select(_ => new GameWidgetDesignViewModel())
            .ToArray();
        
        var supportedGames = Enumerable.Range(0, 3)
            .Select(_ => new MiniGameWidgetDesignViewModel())
            .ToList();
        
        supportedGames.Add(new MiniGameWidgetDesignViewModel { Placeholder = true });
        
        DetectedGames = new ReadOnlyObservableCollection<IGameWidgetViewModel>(new ObservableCollection<IGameWidgetViewModel>(managedGames));
        SupportedGames = new ReadOnlyObservableCollection<IMiniGameWidgetViewModel>(new ObservableCollection<IMiniGameWidgetViewModel>(supportedGames));
    }
}
