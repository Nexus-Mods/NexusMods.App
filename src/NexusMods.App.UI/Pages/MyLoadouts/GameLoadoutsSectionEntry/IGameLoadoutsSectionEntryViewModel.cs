using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.LoadoutCard;

namespace NexusMods.App.UI.Pages.MyLoadouts.GameLoadoutsSectionEntry;

public interface IGameLoadoutsSectionEntryViewModel : IViewModelInterface
{
    string HeadingText { get; }
    ReadOnlyObservableCollection<IViewModelInterface> CardViewModels { get; }
}
