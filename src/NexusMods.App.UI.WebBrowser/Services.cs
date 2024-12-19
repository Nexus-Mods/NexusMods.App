using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.OAuth;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.WebBrowser;

public static class Services
{
    public static IServiceCollection AddWebBrowser(this IServiceCollection services)
    {
        services.AddViewModel<WebBrowserPageViewModel, IWebBrowserPageViewModel>();
        services.AddView<WebBrowserPageView, IWebBrowserPageViewModel>();
        services.AddSingleton<IPageFactory, WebBrowserPageFactory>();
        services.AddSingleton<IOAuthUserInterventionHandler, OAuthLoginBrowserHandler>();
        return services;
    }
    
}
