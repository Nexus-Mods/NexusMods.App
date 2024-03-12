using JetBrains.Annotations;
using NexusMods.App.UI.Windows;

namespace NexusMods.App.UI.WorkspaceSystem;

[PublicAPI]
public readonly struct PageIdBundle
{
    public readonly WindowId WindowId;
    public readonly WorkspaceId WorkspaceId;
    public readonly PanelId PanelId;
    public readonly PanelTabId TabId;

    public PageIdBundle(WindowId windowId, WorkspaceId workspaceId, PanelId panelId, PanelTabId tabId)
    {
        WindowId = windowId;
        WorkspaceId = workspaceId;
        PanelId = panelId;
        TabId = tabId;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{{ Window={WindowId}, Workspace={WorkspaceId}, Panel={PanelId}, Tab={TabId} }}";
    }
}
