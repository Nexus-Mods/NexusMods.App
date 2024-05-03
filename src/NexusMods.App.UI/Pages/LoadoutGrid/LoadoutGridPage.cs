using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages.LoadoutGrid;

[JsonName("NexusMods.App.UI.Page.LoadoutGridContext")]
public record LoadoutGridContext : IPageFactoryContext
{
    public required LoadoutId LoadoutId { get; init; }
}

[UsedImplicitly]
public class LoadoutGridPageFactory : APageFactory<ILoadoutGridViewModel, LoadoutGridContext>
{
    private readonly IConnection _conn;
    public LoadoutGridPageFactory(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _conn = serviceProvider.GetRequiredService<IConnection>();
    }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("c6221ce6-cf12-49bf-b32c-8138ef701cc5"));
    public override PageFactoryId Id => StaticId;

    public override ILoadoutGridViewModel CreateViewModel(LoadoutGridContext context)
    {
        var vm = ServiceProvider.GetRequiredService<ILoadoutGridViewModel>();
        vm.LoadoutId = context.LoadoutId;
        return vm;
    }

    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        if (workspaceContext is not LoadoutContext loadoutContext) yield break;

        var loadout = _conn.Db.Get(loadoutContext.LoadoutId);
        if (!loadout.Contains(Loadout.Name)) yield break;

        yield return new PageDiscoveryDetails
        {
            // TODO: translations?
            SectionName = "Loadouts",
            ItemName = loadout.Name,
            PageData = new PageData
            {
                FactoryId = Id,
                Context = new LoadoutGridContext
                {
                    LoadoutId = loadout.LoadoutId
                }
            }
        };
    }
}
