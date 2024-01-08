using System.Collections.ObjectModel;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface INewTabPageSectionViewModel : IViewModelInterface
{
    public string SectionName { get; }

    public ReadOnlyObservableCollection<INewTabPageSectionItemViewModel> Items { get; }
}
