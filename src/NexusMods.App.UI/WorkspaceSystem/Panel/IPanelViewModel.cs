namespace NexusMods.App.UI.WorkspaceSystem;

public interface IPanelViewModel : IViewModelInterface
{
    public PanelId Id { get; }

    public IViewModel? Content { get; set; }
}
