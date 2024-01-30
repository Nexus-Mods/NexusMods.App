using System.Collections.ObjectModel;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IAddPanelDropDownViewModel
{
    public ReadOnlyObservableCollection<IAddPanelButtonViewModel> AddPanelIconViewModels { get; }

    public IAddPanelButtonViewModel SelectedAddPanelButtonViewModel { get; set; }
}
