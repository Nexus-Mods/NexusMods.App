using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.MyLoadouts;

[JsonName("NexusMods.App.UI.Pages.MyLoadoutsPageContext")]
public record MyLoadoutsPageContext : IPageFactoryContext;

[UsedImplicitly]
public class MyLoadoutsPageFactory : APageFactory<IMyLoadoutsViewModel, MyLoadoutsPageContext>
{
    public MyLoadoutsPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("AF0C54D6-04AE-4F80-812E-1DB31A599C58"));
    public override PageFactoryId Id => StaticId;

    public override IMyLoadoutsViewModel CreateViewModel(MyLoadoutsPageContext context)
    {
        return ServiceProvider.GetRequiredService<IMyLoadoutsViewModel>();
    }
}

