using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.App.UI.Pages.CollectionDownload;

[JsonName(nameof(CollectionDownloadPageContext))]
public record CollectionDownloadPageContext : IPageFactoryContext
{
    public required LoadoutId TargetLoadout { get; init; }
    public required CollectionRevisionMetadataId CollectionRevisionMetadataId { get; init; }
}

[UsedImplicitly]
public class CollectionDownloadPageFactory : APageFactory<ICollectionDownloadViewModel, CollectionDownloadPageContext>
{
    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("50790b33-41cb-432e-a877-4730d2b3c13e"));

    private readonly IConnection _connection;
    public CollectionDownloadPageFactory(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
    }

    public override PageFactoryId Id => StaticId;

    public override ICollectionDownloadViewModel CreateViewModel(CollectionDownloadPageContext context)
    {
        var metadata = CollectionRevisionMetadata.Load(_connection.Db, context.CollectionRevisionMetadataId);
        if (!metadata.IsValid()) throw new InvalidOperationException($"{nameof(CollectionRevisionMetadata)} is invalid for `{context.CollectionRevisionMetadataId}`");

        return new CollectionDownloadViewModel(
            windowManager: WindowManager,
            serviceProvider: ServiceProvider,
            revisionMetadata: metadata,
            targetLoadout: context.TargetLoadout
        );
    }
}
