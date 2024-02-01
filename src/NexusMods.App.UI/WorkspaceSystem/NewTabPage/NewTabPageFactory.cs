using JetBrains.Annotations;

namespace NexusMods.App.UI.WorkspaceSystem;

[UsedImplicitly]
public record NewTabPageContext : IPageFactoryContext
{
    public required PageDiscoveryDetails[] DiscoveryDetails { get; init; }
}

[UsedImplicitly]
public class NewTabPageFactory : APageFactory<INewTabPageViewModel, NewTabPageContext>
{
    public NewTabPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("81b4b072-87bc-4b1f-9764-178eae0c0bf9"));
    public override PageFactoryId Id => StaticId;

    public override INewTabPageViewModel CreateViewModel(NewTabPageContext context)
    {
        return new NewTabPageViewModel(WindowManager, context.DiscoveryDetails);
    }
}
