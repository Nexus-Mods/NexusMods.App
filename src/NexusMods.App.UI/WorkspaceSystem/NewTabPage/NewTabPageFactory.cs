using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.WorkspaceSystem;

[UsedImplicitly]
[JsonName("NexusMods.App.UI.Workspace.NewTabPageContext")]
public record NewTabPageContext : IPageFactoryContext
{
    [JsonIgnore]
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
        return new NewTabPageViewModel(
            ServiceProvider.GetRequiredService<ISettingsManager>(),
            WindowManager,
            context.DiscoveryDetails
        );
    }
}
