using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.WorkspaceSystem;

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
        return ServiceProvider.GetRequiredService<IFileOriginsPageViewModel>();
    }

    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        if (workspaceContext is not LoadoutContext loadoutContext) yield break;

        yield return new PageDiscoveryDetails
        {
            SectionName = "Downloads",
            ItemName = "My mods",
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
