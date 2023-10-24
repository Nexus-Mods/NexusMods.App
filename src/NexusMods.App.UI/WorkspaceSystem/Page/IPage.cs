namespace NexusMods.App.UI.WorkspaceSystem;

public interface IPage
{
    public IViewModel? ViewModel { get; set; }

    public APageData PageData { get; set; }
}
