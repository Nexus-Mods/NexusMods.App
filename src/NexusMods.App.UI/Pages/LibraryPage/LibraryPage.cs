using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Settings;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;

namespace NexusMods.App.UI.Pages.LibraryPage;

[JsonName("NexusMods.App.UI.Pages.Library.LibraryPageContext")]
public record LibraryPageContext : IPageFactoryContext
{
    public required LoadoutId LoadoutId { get; init; }
}

[UsedImplicitly]
public class LibraryPageFactory : APageFactory<ILibraryViewModel, LibraryPageContext>
{
    public LibraryPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("547926e3-56ba-4ed1-912d-d0d7e8b7e287"));
    public override PageFactoryId Id => StaticId;

    public override ILibraryViewModel CreateViewModel(LibraryPageContext context)
    {
        var vm = new LibraryViewModel(
            ServiceProvider.GetRequiredService<IWindowManager>(), 
            ServiceProvider,
            ServiceProvider.GetRequiredService<IGameDomainToGameIdMappingCache>(),
            ServiceProvider.GetRequiredService<INexusApiClient>(),
            context.LoadoutId);
        return vm;
    }

    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        if (workspaceContext is not LoadoutContext loadoutContext) yield break;

        yield return new PageDiscoveryDetails
        {
            SectionName = "Mods",
            ItemName = Language.LibraryPageTitle,
            Icon = IconValues.LibraryOutline,
            PageData = new PageData
            {
                FactoryId = Id,
                Context = new LibraryPageContext
                {
                    LoadoutId = loadoutContext.LoadoutId,
                },
            },
        };
    }
}
