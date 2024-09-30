using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages.CollectionDownload;

[JsonName("NexusMods.App.UI.Pages.CollectionDownload.CollectionDownloadPageContext")]
public record CollectionDownloadPageContext : IPageFactoryContext
{
    /// <summary>
    /// The collection revision ID (a globally unique identifier).
    /// </summary>
    public required RevisionId RevisionId { get; init; }
    
    /// <summary>
    /// The loadout id into which the collection will be eventually installed.
    /// </summary>
    public required LoadoutId LoadoutId { get; init; }
}

[UsedImplicitly]
public class CollectionDownloadPageFactory : APageFactory<ICollectionDownloadViewModel, CollectionDownloadPageContext>
{
    private readonly IServiceProvider _serviceProvider;

    public CollectionDownloadPageFactory(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("346F885B-C517-48A4-AF54-F66D6972E855"));
    public override PageFactoryId Id => StaticId;

    public override ICollectionDownloadViewModel CreateViewModel(CollectionDownloadPageContext context)
    {
        var vm = new CollectionDownloadViewModel(_serviceProvider.GetRequiredService<IWindowManager>(), 
            _serviceProvider,
            context);
        return vm;
    }
    
    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        yield break;
    }
}
