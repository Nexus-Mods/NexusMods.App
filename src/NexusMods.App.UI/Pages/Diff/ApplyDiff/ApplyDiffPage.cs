using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Diff.ApplyDiff;


public record ApplyDiffPageContext : IPageFactoryContext
{
    public required LoadoutId LoadoutId { get; init; }
}

[UsedImplicitly]
public class ApplyDiffPageFactory : APageFactory<IApplyDiffViewModel, ApplyDiffPageContext>
{
    private readonly ILoadoutRegistry _loadoutRegistry;
    public ApplyDiffPageFactory(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _loadoutRegistry = serviceProvider.GetRequiredService<ILoadoutRegistry>();
    }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("db77a8c2-61ad-4d59-8e95-4bebbba9ea5f"));
    public override PageFactoryId Id => StaticId;

    public override IApplyDiffViewModel CreateViewModel(ApplyDiffPageContext context)
    {
        var vm = ServiceProvider.GetRequiredService<IApplyDiffViewModel>();
        vm.LoadoutId = context.LoadoutId;
        return vm;
    }

    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        if (workspaceContext is not LoadoutContext loadoutContext) yield break;

        var loadout = _loadoutRegistry.Get(loadoutContext.LoadoutId);
        if (loadout is null) yield break;

        yield return new PageDiscoveryDetails
        {
            SectionName = "Utilities",
            ItemName = "Preview Apply Changes",
            PageData = new PageData
            {
                FactoryId = StaticId,
                Context = new ApplyDiffPageContext
                {
                    LoadoutId = loadout.LoadoutId,
                },
            },
        };
    }

}
