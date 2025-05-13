using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages.RestoreLoadout;

[JsonName("NexusMods.App.UI.Pages.RestoreLoadoutPageContext")]
public record RestoreLoadoutPageContext(EntityId LoadoutId) : IPageFactoryContext;


public class RestoreLoadoutPageFactory : APageFactory<IRestoreLoadoutViewModel, RestoreLoadoutPageContext>
{
    public RestoreLoadoutPageFactory(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("A946A944-76D5-438D-BE03-90A9A5E73A8A"));
    public override PageFactoryId Id => StaticId;    
    public override IRestoreLoadoutViewModel CreateViewModel(RestoreLoadoutPageContext context)
    {
        var vm = ServiceProvider.GetRequiredService<IRestoreLoadoutViewModel>();
        vm.LoadoutId = context.LoadoutId;
        return vm;
    }
}
