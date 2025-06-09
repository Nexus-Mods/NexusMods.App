using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Pages;

[JsonName(nameof(ProtocolRegistrationTestPageContext))]
public record ProtocolRegistrationTestPageContext : IPageFactoryContext;

public class ProtocolRegistrationTestPageFactory : APageFactory<IProtocolRegistrationTestPageViewModel, ProtocolRegistrationTestPageContext>
{
    public ProtocolRegistrationTestPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override PageFactoryId Id => StaticId;
    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("3C1B0632-8407-4904-8F30-5B86067E9715"));

    public override IProtocolRegistrationTestPageViewModel CreateViewModel(ProtocolRegistrationTestPageContext context)
    {
        return new ProtocolRegistrationTestPageViewModel(ServiceProvider, WindowManager);
    }

    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        return
        [
            new PageDiscoveryDetails
            {
                Icon = IconValues.Cog,
                ItemName = "Protocol Registration Test",
                SectionName = "Utilities",
                PageData = new PageData
                {
                    Context = new ProtocolRegistrationTestPageContext(),
                    FactoryId = StaticId,
                },
            },
        ];
    }
}
