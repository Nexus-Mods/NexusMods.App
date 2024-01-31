using NexusMods.App.UI.Windows;

namespace NexusMods.App.UI.WorkspaceSystem;

public abstract class APageViewModel<TInterface> : AViewModel<TInterface>, IPageViewModelInterface
    where TInterface : class, IPageViewModelInterface
{
    private readonly IWorkspaceController _workspaceController;

    protected APageViewModel(IWorkspaceController workspaceController)
    {
        _workspaceController = workspaceController;
    }

    protected IWorkspaceController GetWorkspaceController()
    {
        return _workspaceController;
    }

    /// <inheritdoc/>
    public WindowId WindowId { get; set; }

    /// <inheritdoc/>
    public WorkspaceId WorkspaceId { get; set; }

    /// <inheritdoc/>
    public PanelId PanelId { get; set; }

    /// <inheritdoc/>
    public PanelTabId TabId { get; set; }
}
