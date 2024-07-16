using System.Collections.ObjectModel;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls.LoadoutCard;

namespace NexusMods.App.UI.Pages.MyLoadouts.GameLoadoutsSectionEntry;

public class GameLoadoutsSectionEntryViewModel : AViewModel<IGameLoadoutsSectionEntryViewModel>, IGameLoadoutsSectionEntryViewModel
{
    public GameLoadoutsSectionEntryViewModel(GameInstallation gameInstallation)
    {
        HeadingText = gameInstallation.Game.Name + " Loadouts";
    }
    public string HeadingText { get; }
    public ReadOnlyObservableCollection<IViewModelInterface> CardVMs { get; } = new([]);
}
