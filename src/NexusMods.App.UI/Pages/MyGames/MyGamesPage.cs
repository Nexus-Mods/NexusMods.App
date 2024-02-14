using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.MyGames;

[JsonName("NexusMods.App.UI.Pages.MyGamesPageContext")]
public record MyGamesPageContext : IPageFactoryContext;

[UsedImplicitly]
public class MyGamesPageFactory : APageFactory<IMyGamesViewModel, MyGamesPageContext>
{
    public MyGamesPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("aa75df24-20e8-459d-a1cd-bb757728c019"));
    public override PageFactoryId Id => StaticId;

    public override IMyGamesViewModel CreateViewModel(MyGamesPageContext context)
    {
        return ServiceProvider.GetRequiredService<IMyGamesViewModel>();
    }
}
