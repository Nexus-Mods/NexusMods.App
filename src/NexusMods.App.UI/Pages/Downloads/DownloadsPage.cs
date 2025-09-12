using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Downloads;

[JsonName("NexusMods.App.UI.Pages.Downloads.DownloadsPageContext")]
public record DownloadsPageContext : IPageFactoryContext;

[JsonName("NexusMods.App.UI.Pages.Downloads.AllDownloadsPageContext")]
public record AllDownloadsPageContext : DownloadsPageContext;

[JsonName("NexusMods.App.UI.Pages.Downloads.CompletedDownloadsPageContext")]
public record CompletedDownloadsPageContext : DownloadsPageContext;

[JsonName("NexusMods.App.UI.Pages.Downloads.GameSpecificDownloadsPageContext")]
public record GameSpecificDownloadsPageContext(GameId GameId) : DownloadsPageContext;

[UsedImplicitly]
public class DownloadsPageFactory(IServiceProvider serviceProvider) : APageFactory<IDownloadsPageViewModel, DownloadsPageContext>(serviceProvider)
{
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