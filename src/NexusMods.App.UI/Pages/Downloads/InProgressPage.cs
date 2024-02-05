using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Downloads;

[JsonName("NexusMods.App.UI.Pages.InProgressPageContext")]
public record InProgressPageContext : IPageFactoryContext;

[UsedImplicitly]
public class InProgressPageFactory : APageFactory<IInProgressViewModel, InProgressPageContext>
{
    public InProgressPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("1ca0baaf-725a-47fb-8596-2567734a4113"));
    public override PageFactoryId Id => StaticId;

    public override IInProgressViewModel CreateViewModel(InProgressPageContext context)
    {
        return ServiceProvider.GetRequiredService<IInProgressViewModel>();
    }

    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        yield return new PageDiscoveryDetails
        {
            // TODO: translations?
            SectionName = "Downloads",
            ItemName = "In-progress Downloads",
            PageData = new PageData
            {
                Context = new InProgressPageContext(),
                FactoryId = Id
            }
        };
    }
}
