using System.Collections.ObjectModel;
using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Controls.MiniGameWidget;
using NexusMods.App.UI.Controls.MiniGameWidget.Standard;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.MyGames;

public interface IMyGamesViewModel : IPageViewModelInterface
{
    public ReactiveCommand<Unit, Unit> OpenRoadmapCommand { get; }
    
    public ReadOnlyObservableCollection<IGameWidgetViewModel> InstalledGames { get; }
    
    public ReadOnlyObservableCollection<IViewModelInterface> SupportedGames { get; }

}
