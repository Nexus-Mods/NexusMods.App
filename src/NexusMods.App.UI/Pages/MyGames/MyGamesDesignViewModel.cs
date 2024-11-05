using System.Collections.ObjectModel;
using System.Reactive;
using DynamicData;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Controls.MiniGameWidget;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.MyGames;

public class MyGamesDesignViewModel : APageViewModel<IMyGamesViewModel>, IMyGamesViewModel
{
    public ReactiveCommand<Unit, Unit> GiveFeedbackCommand => Initializers.DisabledReactiveCommand;
    public ReactiveCommand<Unit, Unit> OpenRoadmapCommand => Initializers.DisabledReactiveCommand;
    public ReadOnlyObservableCollection<IGameWidgetViewModel> InstalledGames { get; }
    public ReadOnlyObservableCollection<IMiniGameWidgetViewModel> SupportedGames { get; }

    public MyGamesDesignViewModel() : base(new DesignWindowManager())
    {
        var detectedGames = Enumerable.Range(0, 2)
            .Select(_ => new GameWidgetDesignViewModel())
            .ToArray();
        
        var supportedGames = Enumerable.Range(0, 3)
            .Select(_ => new MiniGameWidgetDesignViewModel())
            .ToList();
        
        InstalledGames = new ReadOnlyObservableCollection<IGameWidgetViewModel>(new ObservableCollection<IGameWidgetViewModel>(detectedGames));
        SupportedGames = new ReadOnlyObservableCollection<IMiniGameWidgetViewModel>(new ObservableCollection<IMiniGameWidgetViewModel>(supportedGames));
    }
}
