using NexusMods.Abstractions.OAuth;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.WebBrowser;

public interface IWebBrowserPageViewModel : IPageViewModelInterface
{
    /// <summary>
    /// A OAuth login request to be displayed in the web browser.
    /// </summary>
    public OAuthLoginRequest OAuthLoginRequest { get; set; }

    /// <summary>
    /// The address to be displayed in the web browser.
    /// </summary>
    public Uri Address { get; }

    /// <summary>
    /// Called by the view whenever the user navigates to a new URI.
    /// </summary>
    public void NavigatedTo(Uri uri);
}
