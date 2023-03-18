using System.Windows.Input;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.Home;

/// <summary>
/// Designer view model for the Home page.
/// </summary>
public class HomeViewDesignerViewModel : AViewModel<IHomeViewModel>, IHomeViewModel
{
    // <inheritdoc/>
    [Reactive]
    public IFoundGamesViewModel FoundGames { get; set; } =
        new FoundGamesDesignViewModel();

    // <inheritdoc/>
    [Reactive]
    public ICommand BrowseAllGamesCommand { get; set; } = Initializers.ICommand;
}
