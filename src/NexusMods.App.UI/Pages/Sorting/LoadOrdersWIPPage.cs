using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;

namespace NexusMods.App.UI.Pages.Sorting;

[JsonName("NexusMods.App.UI.Pages.Sorting.LoadOrdersWIPPageContext")]
public record LoadOrdersWIPPageContext : IPageFactoryContext
{
    public required LoadoutId LoadoutId { get; init; }
}

[UsedImplicitly]
public class LoadOrdersWIPPageFactory : APageFactory<ILoadOrdersWIPPageViewModel, LoadOrdersWIPPageContext>
{
    public LoadOrdersWIPPageFactory(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("5192B4BE-4DEF-4C99-BDB9-32AEAF70D9A8"));
    public override PageFactoryId Id => StaticId;

    public override ILoadOrdersWIPPageViewModel CreateViewModel(LoadOrdersWIPPageContext context)
    {
        var vm = new LoadOrdersWipPageViewModel(ServiceProvider.GetRequiredService<IWindowManager>(), ServiceProvider, context.LoadoutId);
        return vm;
    }

    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        if (workspaceContext is not LoadoutContext loadoutContext) yield break;

        yield return new PageDiscoveryDetails
        {
            SectionName = "Mods",
            ItemName = "Load Orders (WIP)",
            Icon = IconValues.Swap,
            PageData = new PageData
            {
                FactoryId = Id,
                Context = new LoadOrdersWIPPageContext
                {
                    LoadoutId = loadoutContext.LoadoutId,
                }
            }
        };
    }
}
