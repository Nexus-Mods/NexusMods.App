using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.BuildInfo;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using R3;

namespace NexusMods.App.UI.Pages.ObservableInfo;

[JsonName("ObservableInfoPageContext")]
public record ObservableInfoPageContext : IPageFactoryContext;

[UsedImplicitly]
public class ObservableInfoPageFactory : APageFactory<IObservableInfoPageViewModel, ObservableInfoPageContext>
{
    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("2d0dce70-c5a7-4bea-b985-78007cdd95e6"));
    public override PageFactoryId Id => StaticId;

    public ObservableInfoPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override IObservableInfoPageViewModel CreateViewModel(ObservableInfoPageContext context)
    {
        ObservableTracker.EnableTracking = true;
        ObservableTracker.EnableStackTrace = true;
        return new ObservableInfoPageViewModel(WindowManager);
    }

    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        if (!CompileConstants.IsDebug) return [];

        return
        [
            new PageDiscoveryDetails
            {
                Icon = IconValues.Warning,
                ItemName = "Observable Tracker",
                SectionName = "Utilities",
                PageData = new PageData
                {
                    FactoryId = StaticId,
                    Context = new ObservableInfoPageContext(),
                },
            },
        ];
    }
}
