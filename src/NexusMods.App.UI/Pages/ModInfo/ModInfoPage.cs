using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.Pages.ModInfo.Types;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.ModInfo;

[JsonName("NexusMods.App.UI.Pages.ViewModInfo.ModInfoPageContext")]
public record ModInfoPageContext : IPageFactoryContext
{
    public required LoadoutId LoadoutId { get; init; }
    public required ModId ModId { get; init; }
    public required CurrentModInfoSection Section { get; init; }
}

[UsedImplicitly]
public class ModInfoPageFactory : APageFactory<IModInfoViewModel, ModInfoPageContext>
{
    public ModInfoPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("12ac717d-fa73-4dc0-a23b-17fd5410b065"));
    public override PageFactoryId Id => StaticId;

    public override IModInfoViewModel CreateViewModel(ModInfoPageContext context)
    {
        var vm = ServiceProvider.GetRequiredService<IModInfoViewModel>();
        vm.SetContext(context);
        return vm;
    }
}

