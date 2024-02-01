using System.Collections.ObjectModel;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IAddPanelDropDownViewModel : IViewModelInterface
{
    public ReadOnlyObservableCollection<IAddPanelButtonViewModel> AddPanelIconViewModels { get; }

    public int SelectedIndex { get; set; }
}
