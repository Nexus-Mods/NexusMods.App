using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Settings;

[JsonName("NexusMods.App.UI.Pages.SettingsPageContext")]
public record SettingsPageContext : IPageFactoryContext;

[UsedImplicitly]
public class SettingsPageFactory : APageFactory<ISettingsViewModel, SettingsPageContext>
{
    public SettingsPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("3DE311A0-0AB0-4191-9CA5-5CE8EA76C393"));
    public override PageFactoryId Id => StaticId;

    public override ISettingsViewModel CreateViewModel(SettingsPageContext context)
    {
        return ServiceProvider.GetRequiredService<ISettingsViewModel>();
    }
}
