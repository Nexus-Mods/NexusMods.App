using System.Collections.ObjectModel;
using NexusMods.App.UI.Pages.MyLoadouts.GameLoadoutsSectionEntry;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.MyLoadouts;


public interface IMyLoadoutsViewModel : IPageViewModelInterface
{
    ReadOnlyObservableCollection<IGameLoadoutsSectionEntryViewModel> GameSectionViewModels { get; }
}
