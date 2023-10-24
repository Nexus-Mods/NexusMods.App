namespace NexusMods.App.UI.WorkspaceSystem;

public record Page
{
    public IViewModel? ViewModel { get; set; }

    public required PageData PageData { get; set; }
}
