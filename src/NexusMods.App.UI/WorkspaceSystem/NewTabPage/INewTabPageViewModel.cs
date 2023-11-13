using System.Collections.ObjectModel;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface INewTabPageViewModel : IViewModelInterface
{
    public ReadOnlyObservableCollection<INewTabPageSectionViewModel> Sections { get; }
}
