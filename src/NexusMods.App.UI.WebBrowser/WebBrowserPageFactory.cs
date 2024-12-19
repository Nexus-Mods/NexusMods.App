using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.OAuth;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.WebBrowser;

public record WebBrowserPageContext : IPageFactoryContext
{
    public required OAuthLoginRequest OAuthLoginRequest { get; init; }
    public bool IsEphemeral => true;
}

public class WebBrowserPageFactory : APageFactory<IWebBrowserPageViewModel, WebBrowserPageContext>
{
    public WebBrowserPageFactory(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
    
    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("5B6C1EC9-A78D-4949-9EF7-83030D164371"));
    public override PageFactoryId Id => StaticId;
    public override IWebBrowserPageViewModel CreateViewModel(WebBrowserPageContext context)
    {
        var vm = ServiceProvider.GetRequiredService<IWebBrowserPageViewModel>();
        vm.OAuthLoginRequest = context.OAuthLoginRequest;
        return vm;
    }
}
