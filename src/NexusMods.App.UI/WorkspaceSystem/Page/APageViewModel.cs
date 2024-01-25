namespace NexusMods.App.UI.WorkspaceSystem;

public abstract class APageViewModel<TInterface> : AViewModel<TInterface>, IPageViewModelInterface
    where TInterface : class, IPageViewModelInterface
{
    /// <inheritdoc/>
    public IWorkspaceController WorkspaceController { get; set; } = null!;

    /// <inheritdoc/>
    public PanelId PanelId { get; set; }

    /// <inheritdoc/>
    public PanelTabId TabId { get; set; }
}
