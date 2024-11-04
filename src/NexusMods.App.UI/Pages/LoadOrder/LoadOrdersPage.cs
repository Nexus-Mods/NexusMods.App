using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;

namespace NexusMods.App.UI.Pages.LoadOrder;

[JsonName("NexusMods.App.UI.Pages.LoadOrder.LoadOrdersPageContext")]
public record LoadOrdersPageContext : IPageFactoryContext
{
    public required LoadoutId LoadoutId { get; init; }
}

[UsedImplicitly]
public class LoadOrdersPageFactory : APageFactory<ILoadOrdersPageViewModel, LoadOrdersPageContext>
{
    public LoadOrdersPageFactory(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("5192B4BE-4DEF-4C99-BDB9-32AEAF70D9A8"));
    public override PageFactoryId Id => StaticId;

    public override ILoadOrdersPageViewModel CreateViewModel(LoadOrdersPageContext context)
    {
        var vm = new LoadOrdersPageViewModel(ServiceProvider.GetRequiredService<IWindowManager>(), ServiceProvider, context.LoadoutId);
        return vm;
    }

    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        if (workspaceContext is not LoadoutContext loadoutContext) yield break;

        yield return new PageDiscoveryDetails
        {
            SectionName = "Mods",
            ItemName = "Load Orders (WIP)",
            Icon = IconValues.Collections,
            PageData = new PageData
            {
                FactoryId = Id,
                Context = new LoadOrdersPageContext
                {
                    LoadoutId = loadoutContext.LoadoutId,
                }
            }
        };
    }
}
