namespace NexusMods.App.UI.WorkspaceSystem;

public record Page
{
    public required IPageViewModelInterface ViewModel { get; set; }

    public required PageData PageData { get; set; }
}
