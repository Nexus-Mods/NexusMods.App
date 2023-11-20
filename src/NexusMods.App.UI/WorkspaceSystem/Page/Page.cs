namespace NexusMods.App.UI.WorkspaceSystem;

public record Page
{
    public IPageViewModelInterface? ViewModel { get; set; }

    public required PageData PageData { get; set; }
}
