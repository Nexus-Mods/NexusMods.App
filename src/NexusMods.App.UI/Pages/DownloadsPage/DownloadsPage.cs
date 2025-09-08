using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.DownloadsPage;

[JsonName("NexusMods.App.UI.Pages.Downloads.DownloadsPageContext")]
public record DownloadsPageContext : IPageFactoryContext;

[UsedImplicitly]
public class DownloadsPageFactory : APageFactory<IDownloadsPageViewModel, DownloadsPageContext>
{
    public DownloadsPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("c8f7a2e1-9b4d-4c3a-8f6e-1a2b3c4d5e6f"));
    public override PageFactoryId Id => StaticId;

    public override IDownloadsPageViewModel CreateViewModel(DownloadsPageContext context)
    {
        return new DownloadsPageViewModel(
            ServiceProvider.GetRequiredService<IWindowManager>(),
            ServiceProvider.GetRequiredService<IDownloadsService>()
        );
    }
}