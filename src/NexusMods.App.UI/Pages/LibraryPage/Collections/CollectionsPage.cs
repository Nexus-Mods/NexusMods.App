using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages.LibraryPage.Collections;

[JsonName("NexusMods.App.UI.Pages.Library.Collections.CollectionsPageContext")]
public record CollectionsPageContext : IPageFactoryContext
{
    public required LoadoutId LoadoutId { get; init; }
}

[UsedImplicitly]
public class CollectionsPageFactory : APageFactory<ICollectionsViewModel, CollectionsPageContext>
{
    private readonly ISettingsManager _settingsManager;
    public CollectionsPageFactory(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _settingsManager = serviceProvider.GetRequiredService<ISettingsManager>();
    }
    

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("06854703-E839-4CE6-9B3F-C6299FC446D6"));
    public override PageFactoryId Id => StaticId;

    public override ICollectionsViewModel CreateViewModel(CollectionsPageContext context)
    {
        var vm = new CollectionsViewModel(ServiceProvider.GetRequiredService<IConnection>(),
            ServiceProvider.GetRequiredService<IWindowManager>(), context.LoadoutId);
        return vm;
    }

    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        if (workspaceContext is not LoadoutContext loadoutContext) yield break;

        yield return new PageDiscoveryDetails
        {
            SectionName = "Mods",
            ItemName = "Collections (WIP)",
            Icon = IconValues.ModLibrary,
            PageData = new PageData
            {
                FactoryId = Id,
                Context = new CollectionsPageContext
                {
                    LoadoutId = loadoutContext.LoadoutId,
                },
            },
        };
    }
}
