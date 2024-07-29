using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.LoadoutGroupFiles;

[JsonName("NexusMods.App.UI.Pages.LoadoutGroupFilesPageContext")]
public record LoadoutGroupFilesPageContext : IPageFactoryContext
{
    public required LoadoutItemGroupId GroupId { get; init; }
}

[UsedImplicitly]
public class LoadoutGroupFilesPageFactory : APageFactory<ILoadoutGroupFilesViewModel, LoadoutGroupFilesPageContext>
{
    public LoadoutGroupFilesPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("85d19521-0414-4a31-a873-43757234ed43"));
    public override PageFactoryId Id => StaticId;

    public override ILoadoutGroupFilesViewModel CreateViewModel(LoadoutGroupFilesPageContext context)
    {
        var vm = ServiceProvider.GetRequiredService<ILoadoutGroupFilesViewModel>();
        vm.Context = context;
        return vm;
    }
}
