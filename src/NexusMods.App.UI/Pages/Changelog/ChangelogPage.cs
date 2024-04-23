using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Changelog;

[JsonName("NexusMods.App.UI.Pages.Changelog.ChangelogPageContext")]
public record ChangelogPageContext : IPageFactoryContext
{
    public required Version? TargetVersion { get; init; }
}

[UsedImplicitly]
public class ChangelogPageFactory : APageFactory<IChangelogPageViewModel, ChangelogPageContext>
{
    public ChangelogPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("91b9bdb1-bd81-4407-af12-3ac35f05ab20"));
    public override PageFactoryId Id => StaticId;

    public override IChangelogPageViewModel CreateViewModel(ChangelogPageContext context)
    {
        var vm = ServiceProvider.GetRequiredService<IChangelogPageViewModel>();
        vm.TargetVersion = context.TargetVersion;

        return vm;
    }

    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        return new[]
        {
            new PageDiscoveryDetails
            {
                // TODO:
                SectionName = "TODO",
                ItemName = "Changelog",
                PageData = new PageData
                {
                    // TODO: get current version
                    Context = new ChangelogPageContext
                    {
                        TargetVersion = null,
                    },
                    FactoryId = StaticId,
                },
            },
        };
    }
}
