using Avalonia.Media;
using JetBrains.Annotations;

namespace NexusMods.App.UI.WorkspaceSystem;

[PublicAPI]
public interface ITabController
{
    /// <summary>
    /// Sets the title of the current tab;
    /// </summary>
    public void SetTitle(string title);

    /// <summary>
    /// Sets the icon of the current tab.
    /// </summary>
    public void SetIcon(IImage? icon);
}
