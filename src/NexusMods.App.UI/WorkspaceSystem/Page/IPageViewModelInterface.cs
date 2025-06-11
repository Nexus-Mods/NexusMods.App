using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Windows;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.WorkspaceSystem;

[PublicAPI]
public interface IPageViewModelInterface : IViewModelInterface
{
    /// <summary>
    /// Gets or sets the icon of this page to be shown in the tab header.
    /// </summary>
    IconValue TabIcon { get; }

    /// <summary>
    /// Gets the title of this page in the tab header.
    /// </summary>
    string TabTitle { get; }

    /// <summary>
    /// Gets or sets the ID of the window this page is in.
    /// </summary>
    WindowId WindowId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the workspace this page is in.
    /// </summary>
    WorkspaceId WorkspaceId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the panel this page is in.
    /// </summary>
    PanelId PanelId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the tab this page is in.
    /// </summary>
    PanelTabId TabId { get; set; }

    /// <summary>
    /// Called before the tab is closed.
    /// </summary>
    /// <remarks>
    /// Use this method for pages that might contain unsaved data or need to run
    /// logic on close.
    /// </remarks>
    /// <returns><c>true</c> if the tab can be closed, <c>false</c> if not.</returns>
    bool CanClose();
}
