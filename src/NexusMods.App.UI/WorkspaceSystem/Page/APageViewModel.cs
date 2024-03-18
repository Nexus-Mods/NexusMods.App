using JetBrains.Annotations;
using NexusMods.App.UI.Windows;

namespace NexusMods.App.UI.WorkspaceSystem;

[PublicAPI]
public abstract class APageViewModel<TInterface> : AViewModel<TInterface>, IPageViewModelInterface
    where TInterface : class, IPageViewModelInterface
{
    protected readonly IWindowManager WindowManager;

    protected APageViewModel(IWindowManager windowManager)
    {
        WindowManager = windowManager;
    }

    protected IWorkspaceController GetWorkspaceController()
    {
        if (!WindowManager.TryGetWindow(WindowId, out var window))
        {
            throw new NotImplementedException();
        }

        return window.WorkspaceController;
    }

    /// <inheritdoc/>
    public WindowId WindowId { get; set; }

    /// <inheritdoc/>
    public WorkspaceId WorkspaceId { get; set; }

    /// <inheritdoc/>
    public PanelId PanelId { get; set; }

    /// <inheritdoc/>
    public PanelTabId TabId { get; set; }

    protected PageIdBundle IdBundle => new(WindowId, WorkspaceId, PanelId, TabId);
}
