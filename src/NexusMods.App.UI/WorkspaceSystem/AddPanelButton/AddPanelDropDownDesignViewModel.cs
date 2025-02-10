using NexusMods.Abstractions.UI;

namespace NexusMods.App.UI.WorkspaceSystem;

public class AddPanelDropDownDesignViewModel : AViewModel<IAddPanelDropDownViewModel>, IAddPanelDropDownViewModel
{
    public IReadOnlyList<IAddPanelButtonViewModel> AddPanelButtonViewModels { get; } = new List<IAddPanelButtonViewModel>([
            new AddPanelButtonDesignViewModel(),
            new AddPanelButtonDesignViewModel(),
            new AddPanelButtonDesignViewModel(),
        ]
    );

    public IAddPanelButtonViewModel? SelectedItem { get; set; }
    public int SelectedIndex { get; set; } = 0;
}
