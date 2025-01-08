using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages.Diagnostics;

[JsonName("NexusMods.App.UI.Pages.Diagnostics.DiagnosticListPageContext")]
public record DiagnosticListPageContext : IPageFactoryContext
{
    public required LoadoutId LoadoutId { get; init; }
}

[UsedImplicitly]
public class DiagnosticListPageFactory : APageFactory<IDiagnosticListViewModel, DiagnosticListPageContext>
{
    private readonly IConnection _conn;
    public DiagnosticListPageFactory(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _conn = serviceProvider.GetRequiredService<IConnection>();
    }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("db77a8c2-61ad-4d59-8e95-4bebbba9ea5f"));
    public override PageFactoryId Id => StaticId;

    public override IDiagnosticListViewModel CreateViewModel(DiagnosticListPageContext context)
    {
        var vm = ServiceProvider.GetRequiredService<IDiagnosticListViewModel>();
        vm.LoadoutId = context.LoadoutId;
        return vm;
    }

    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        if (workspaceContext is not LoadoutContext loadoutContext) yield break;

        var loadout = Loadout.Load(_conn.Db, loadoutContext.LoadoutId);

        yield return new PageDiscoveryDetails
        {
            // TODO: translations?
            SectionName = "Utilities",
            ItemName = Language.DiagnosticListViewModel_DiagnosticListViewModel_Diagnostics,
            Icon = IconValues.Cardiology,
            PageData = new PageData
            {
                FactoryId = StaticId,
                Context = new DiagnosticListPageContext
                {
                    LoadoutId = loadout.LoadoutId,
                },
            },
        };
    }
}
