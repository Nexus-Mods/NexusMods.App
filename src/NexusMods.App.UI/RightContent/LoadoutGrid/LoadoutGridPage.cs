using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.App.UI.RightContent.LoadoutGrid;

[UsedImplicitly]
[JsonName("NexusMods.App.UI.RightContent.LoadoutGridParameter")]
public record LoadoutGridParameter : IPageFactoryParameter
{
    public required LoadoutId LoadoutId { get; init; }
}

[UsedImplicitly]
public class LoadoutGridPageFactory : APageFactory<ILoadoutGridViewModel, LoadoutGridParameter>
{
    public LoadoutGridPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }
    public override PageFactoryId Id => PageFactoryId.From(Guid.Parse("c6221ce6-cf12-49bf-b32c-8138ef701cc5"));

    public override ILoadoutGridViewModel CreateViewModel(LoadoutGridParameter parameter)
    {
        var vm = ServiceProvider.GetRequiredService<ILoadoutGridViewModel>();
        vm.LoadoutId = parameter.LoadoutId;
        return vm;
    }
}
