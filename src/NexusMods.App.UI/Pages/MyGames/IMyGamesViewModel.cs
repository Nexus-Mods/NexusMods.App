using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Controls.MiniGameWidget;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.MyGames;

public interface IMyGamesViewModel : IPageViewModelInterface
{
    
    public ReadOnlyObservableCollection<IGameWidgetViewModel> DetectedGames { get; }
    
    public ReadOnlyObservableCollection<IMiniGameWidgetViewModel> SupportedGames { get; }

}
