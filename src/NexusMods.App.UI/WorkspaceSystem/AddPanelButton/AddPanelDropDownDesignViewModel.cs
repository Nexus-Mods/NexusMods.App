using System.Collections.ObjectModel;
using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

public class AddPanelDropDownDesignViewModel :  AViewModel<IAddPanelDropDownViewModel>, IAddPanelDropDownViewModel
{
    public ReadOnlyObservableCollection<IAddPanelButtonViewModel> AddPanelIconViewModels { get; } = new(
    [
        new AddPanelButtonDesignViewModel(),
        new AddPanelButtonDesignViewModel()
    ]);

    public int SelectedIndex { get; set; } = 0;
}
