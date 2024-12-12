using Avalonia.Threading;
using NexusMods.Abstractions.OAuth;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.WebBrowser;

public class OAuthLoginBrowserHandler : IOAuthUserInterventionHandler
{
    private readonly IWindowManager _windowManager;

    public OAuthLoginBrowserHandler(IWindowManager windowManager)
    {
        _windowManager = windowManager;
    }
    
    public Task<Uri?> HandleOAuthRequest(OAuthLoginRequest request, CancellationToken token)
    {
        var pageData = new PageData
        {
            Context = new WebBrowserPageContext
            {
                OAuthLoginRequest = request,
            },
            FactoryId = WebBrowserPageFactory.StaticId,
        };
        var controller = _windowManager.ActiveWorkspaceController;
        Dispatcher.UIThread.Invoke(() =>
            {
                _windowManager.ActiveWorkspaceController.OpenPage(controller.ActiveWorkspaceId, pageData, controller.GetDefaultOpenPageBehavior(pageData, NavigationInput.Default));
            }
        );
        return request.Task;
    }
}
