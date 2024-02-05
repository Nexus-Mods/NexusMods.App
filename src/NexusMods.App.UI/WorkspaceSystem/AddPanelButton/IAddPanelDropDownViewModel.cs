namespace NexusMods.App.UI.WorkspaceSystem;

public interface IAddPanelDropDownViewModel : IViewModelInterface
{
    public IReadOnlyList<IAddPanelButtonViewModel> AddPanelButtonViewModel { get; }

    public IAddPanelButtonViewModel? SelectedItem { get; set; }

    public int SelectedIndex { get; set; }
}
