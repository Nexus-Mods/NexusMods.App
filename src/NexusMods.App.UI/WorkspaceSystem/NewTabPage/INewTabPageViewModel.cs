using System.Collections.ObjectModel;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface INewTabPageViewModel : IPageViewModelInterface
{
    public ReadOnlyObservableCollection<INewTabPageSectionViewModel> Sections { get; }
}
