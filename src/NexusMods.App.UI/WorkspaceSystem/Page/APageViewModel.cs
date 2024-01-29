namespace NexusMods.App.UI.WorkspaceSystem;

public abstract class APageViewModel<TInterface> : AViewModel<TInterface>, IPageViewModelInterface
    where TInterface : class, IPageViewModelInterface
{
    protected readonly IWorkspaceController WorkspaceController;

    protected APageViewModel(IWorkspaceController workspaceController)
    {
        WorkspaceController = workspaceController;
    }

    /// <inheritdoc/>
    public ITabController TabController { get; set; } = null!;

    /// <inheritdoc/>
    public WorkspaceId WorkspaceId { get; set; }

    /// <inheritdoc/>
    public PanelId PanelId { get; set; }

    /// <inheritdoc/>
    public PanelTabId TabId { get; set; }
}
