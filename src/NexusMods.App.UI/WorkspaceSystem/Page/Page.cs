namespace NexusMods.App.UI.WorkspaceSystem;

public record Page
{
    public IViewModelInterface? ViewModel { get; set; }

    public required PageData PageData { get; set; }
}
