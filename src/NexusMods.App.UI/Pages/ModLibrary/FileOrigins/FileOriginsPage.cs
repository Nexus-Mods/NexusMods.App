using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages.ModLibrary;

[JsonName("NexusMods.App.UI.Pages.ModLibrary.FileOrigins.FileOriginsPageContext")]
public record FileOriginsPageContext : IPageFactoryContext
{
    public required LoadoutId LoadoutId { get; init; }
}


[UsedImplicitly]
public class FileOriginsPageFactory : APageFactory<IFileOriginsPageViewModel, FileOriginsPageContext>
{
    public FileOriginsPageFactory(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("B7E092D2-DF06-4329-ABEB-6FEE9373F238"));
    public override PageFactoryId Id => StaticId;

    public override IFileOriginsPageViewModel CreateViewModel(FileOriginsPageContext context)
    {
        return new FileOriginsPageViewModel(
            context.LoadoutId,
            ServiceProvider.GetRequiredService<IArchiveInstaller>(),
            ServiceProvider.GetRequiredService<IRepository<DownloadAnalysis.Model>>(),
            ServiceProvider.GetRequiredService<IConnection>(),
            ServiceProvider.GetRequiredService<IWindowManager>()
        );
    }

    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        if (workspaceContext is not LoadoutContext loadoutContext) yield break;

        yield return new PageDiscoveryDetails
        {
            SectionName = "Downloads",
            ItemName = Language.FileOriginsPageTitle,
            PageData = new PageData
            {
                FactoryId = Id,
                Context = new FileOriginsPageContext
                {
                    LoadoutId = loadoutContext.LoadoutId
                }
            }
        };
    }
}
