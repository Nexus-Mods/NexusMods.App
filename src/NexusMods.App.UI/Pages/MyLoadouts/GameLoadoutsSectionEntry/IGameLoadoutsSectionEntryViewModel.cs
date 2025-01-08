using System.Collections.ObjectModel;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.LoadoutCard;

namespace NexusMods.App.UI.Pages.MyLoadouts.GameLoadoutsSectionEntry;

public interface IGameLoadoutsSectionEntryViewModel : IViewModelInterface, IDisposable
{
    string HeadingText { get; }
    ReadOnlyObservableCollection<IViewModelInterface> CardViewModels { get; }
}
