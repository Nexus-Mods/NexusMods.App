using JetBrains.Annotations;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Icons;

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
        if (WindowId == default(WindowId)) throw new InvalidOperationException("This method is only available in the WhenActivated block");

        if (!WindowManager.TryGetWindow(WindowId, out var window))
        {
            throw new NotImplementedException();
        }

        return window.WorkspaceController;
    }

    private IconValue _tabIcon = new();

    /// <inheritdoc/>
    public IconValue TabIcon
    {
        get => _tabIcon;
        set
        {
            _tabIcon = value;
            if (WindowId == default(WindowId)) return;
            try
            {
                var workspaceController = GetWorkspaceController();
                workspaceController.SetIcon(_tabIcon, WorkspaceId, PanelId, TabId);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    private string _tabTitle = Language.PanelTabHeaderViewModel_Title_New_Tab;

    /// <inheritdoc/>
    public string TabTitle
    {
        get => _tabTitle;
        set
        {
            _tabTitle = value;
            if (WindowId == default(WindowId)) return;
            try
            {
                var workspaceController = GetWorkspaceController();
                workspaceController.SetTabTitle(_tabTitle, WorkspaceId, PanelId, TabId);
            }
            catch (Exception)
            {
                // ignored
            }
        }
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

    /// <inheritdoc/>
    public virtual bool CanClose() => true;
}
