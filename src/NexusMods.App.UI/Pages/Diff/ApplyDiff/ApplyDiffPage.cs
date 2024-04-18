using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Diff.ApplyDiff;

[JsonName("NexusMods.App.UI.Page.ApplyDiffPageContext")]
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

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("CA9E8FFE-02E0-4123-9CA2-F68D07D44583"));
    public override PageFactoryId Id => StaticId;

    public override IApplyDiffViewModel CreateViewModel(ApplyDiffPageContext context)
    {
        var vm = ServiceProvider.GetRequiredService<IApplyDiffViewModel>();
        vm.Initialize(context.LoadoutId);
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
