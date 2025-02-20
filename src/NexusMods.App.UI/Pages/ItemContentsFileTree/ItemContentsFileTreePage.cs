using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.ItemContentsFileTree;

[JsonName("NexusMods.App.UI.Pages.ItemContentsFileTreePageContext")]
public record ItemContentsFileTreePageContext : IPageFactoryContext
{
    public required LoadoutItemGroupId GroupId { get; init; }
    
    public required bool IsReadOnly { get; init; }
}

[UsedImplicitly]
public class ItemContentsFileTreePageFactory : APageFactory<IItemContentsFileTreeViewModel, ItemContentsFileTreePageContext>
{
    public ItemContentsFileTreePageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("85d19521-0414-4a31-a873-43757234ed43"));
    public override PageFactoryId Id => StaticId;

    public override IItemContentsFileTreeViewModel CreateViewModel(ItemContentsFileTreePageContext context)
    {
        var vm = ServiceProvider.GetRequiredService<IItemContentsFileTreeViewModel>();
        vm.Context = context;
        return vm;
    }
}
