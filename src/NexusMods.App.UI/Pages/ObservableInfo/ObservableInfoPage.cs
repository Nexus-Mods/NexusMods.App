using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.BuildInfo;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;

namespace NexusMods.App.UI.Pages.ObservableInfo;

[JsonName("ObservableInfoPageContext")]
public record ObservableInfoPageContext : IPageFactoryContext
{
    public required bool IncludeStackTraces { get; init; }
}

[UsedImplicitly]
public class ObservableInfoPageFactory : APageFactory<IObservableInfoPageViewModel, ObservableInfoPageContext>
{
    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("2d0dce70-c5a7-4bea-b985-78007cdd95e6"));
    public override PageFactoryId Id => StaticId;

    public ObservableInfoPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override IObservableInfoPageViewModel CreateViewModel(ObservableInfoPageContext context)
    {
        return new ObservableInfoPageViewModel(WindowManager, context.IncludeStackTraces);
    }

    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        if (!CompileConstants.IsDebug) return [];

        return
        [
            new PageDiscoveryDetails
            {
                Icon = IconValues.Warning,
                ItemName = "Observable Tracker (with StackTraces)",
                SectionName = "Utilities",
                PageData = new PageData
                {
                    FactoryId = StaticId,
                    Context = new ObservableInfoPageContext
                    {
                        IncludeStackTraces = true,
                    },
                },
            },
            new PageDiscoveryDetails
            {
                Icon = IconValues.Warning,
                ItemName = "Observable Tracker (without StackTraces)",
                SectionName = "Utilities",
                PageData = new PageData
                {
                    FactoryId = StaticId,
                    Context = new ObservableInfoPageContext
                    {
                        IncludeStackTraces = false,
                    },
                },
            },
        ];
    }
}
