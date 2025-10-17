using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.LoadoutCard;
using NexusMods.UI.Sdk;

namespace NexusMods.App.UI.Pages.MyLoadouts.GameLoadoutsSectionEntry;

public interface IGameLoadoutsSectionEntryViewModel : IViewModelInterface, IDisposable
{
    string HeadingText { get; }
    ReadOnlyObservableCollection<IViewModelInterface> CardViewModels { get; }
}
