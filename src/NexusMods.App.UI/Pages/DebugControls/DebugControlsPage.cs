using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.BuildInfo;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using R3;

namespace NexusMods.App.UI.Pages.DebugControls;

[JsonName("DebugControlsPageContext")]
public record DebugControlsPageContext : IPageFactoryContext;

public class DebugControlsPageFactory : APageFactory<IDebugControlsPageViewModel, DebugControlsPageContext>
{
    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("8fb63069-e912-4a10-a46e-3c5048ee5e61"));
    public override PageFactoryId Id => StaticId;

    public DebugControlsPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }
    
    public override IDebugControlsPageViewModel CreateViewModel(DebugControlsPageContext context)
    {
        return new DebugControlsPageViewModel(WindowManager);
    }

    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        if (!CompileConstants.IsDebug) return [];

        return
        [
            new PageDiscoveryDetails
            {
                Icon = IconValues.ColorLens,
                ItemName = "Debug Controls",
                SectionName = "Utilities",
                PageData = new PageData
                {
                    FactoryId = StaticId,
                    Context = new DebugControlsPageContext(),
                },
            },
        ];
    }
}
