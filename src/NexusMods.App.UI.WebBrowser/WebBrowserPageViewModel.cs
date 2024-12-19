using System.Web;
using Avalonia.Threading;
using NexusMods.Abstractions.OAuth;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.WebBrowser;

public class WebBrowserPageViewModel : APageViewModel<IWebBrowserPageViewModel>, IWebBrowserPageViewModel
{
    private OAuthLoginRequest? _oAuthLoginRequest;
    public OAuthLoginRequest OAuthLoginRequest
    {
        set
        {
            _oAuthLoginRequest = value;
            Address = _oAuthLoginRequest.AuthorizationUrl;
        }
        get => _oAuthLoginRequest ?? throw new InvalidOperationException("The LoginRequest is not set.");
    }

    public WebBrowserPageViewModel(IWindowManager windowManager) : base(windowManager)
    {
    }

    public Uri Address { get; set; } = new Uri("https://www.nexusmods.com");
    
    public void NavigatedTo(Uri uri)
    {
        if (_oAuthLoginRequest == null)
            return;

        if (_oAuthLoginRequest.CallbackType != CallbackType.Capture) 
            return;
        
        if (uri.ToString().StartsWith(_oAuthLoginRequest.CallbackPrefix.ToString()))
        {
            Task.Run(() => _oAuthLoginRequest.Callback(uri));
            Dispatcher.UIThread.Invoke(() =>  GetWorkspaceController().ClosePanel(WorkspaceId, PanelId));
        }
    }
}
