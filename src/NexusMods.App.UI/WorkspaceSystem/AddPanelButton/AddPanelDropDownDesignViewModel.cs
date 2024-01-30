using System.Collections.ObjectModel;
using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

public class AddPanelDropDownDesignViewModel : IAddPanelDropDownViewModel
{
    public ReadOnlyObservableCollection<IAddPanelButtonViewModel> AddPanelIconViewModels { get; } = new(
    [
        new AddPanelButtonDesignViewModel(),
        new AddPanelButtonDesignViewModel()
    ]);

    public IAddPanelButtonViewModel SelectedAddPanelButtonViewModel { get; set; } = new AddPanelButtonDesignViewModel();
}
