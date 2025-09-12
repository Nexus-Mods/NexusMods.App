using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Resources;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Resources;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Downloads;

/// <summary>
/// Data provider that transforms IDownloadsService data into CompositeItemModel collections for TreeDataGrid.
/// </summary>
public sealed class DownloadsDataProvider(IServiceProvider serviceProvider) : IDownloadsDataProvider
{
    private readonly IDownloadsService _downloadsService = serviceProvider.GetRequiredService<IDownloadsService>();
    private readonly IGameRegistry _gameRegistry = serviceProvider.GetRequiredService<IGameRegistry>();
    private readonly IConnection _connection = serviceProvider.GetRequiredService<IConnection>();
    private readonly Lazy<IResourceLoader<EntityId, Bitmap>> _thumbnailLoader = new(() => ImagePipelines.GetModPageThumbnailPipeline(serviceProvider));


    public IObservable<IChangeSet<CompositeItemModel<DownloadId>, DownloadId>> ObserveDownloads(DownloadsFilter filter)
    {
        // Select source observable based on filter scope
        var sourceObservable = filter.Scope switch
        {
            DownloadsScope.All => _downloadsService.AllDownloads,
            DownloadsScope.Active => _downloadsService.ActiveDownloads,
            DownloadsScope.Completed => _downloadsService.CompletedDownloads,
            DownloadsScope.GameSpecific when filter.GameId.HasValue => _downloadsService.GetDownloadsForGame(filter.GameId.Value),
            _ => throw new ArgumentException($"Invalid filter scope: {filter.Scope}")
        };

        // Transform each DownloadInfo to CompositeItemModel
        return sourceObservable.Transform(ToItemModel);
    }

    public IObservable<int> CountDownloads(DownloadsFilter filter)
    {
        return ObserveDownloads(filter)
            .QueryWhenChanged(q => q.Count)
            .Prepend(0);
    }

    private CompositeItemModel<DownloadId> ToItemModel(DownloadInfo download)
    {
        var model = new CompositeItemModel<DownloadId>(download.Id);

        // Add name component (Name+Icon column)
        model.Add(SharedColumns.Name.NameComponentKey, new NameComponent(
            initialValue: download.Name,
            valueObservable: download.WhenAnyValue(x => x.Name).ToObservable()));

        // Add icon component for Name+Icon column
        model.Add(SharedColumns.Name.ImageComponentKey, CreateIconComponent(download));

        // Add game component
        model.Add(DownloadColumns.Game.ComponentKey, new DownloadComponents.GameComponent(
            gameName: ResolveGameNameInitial(download.GameId)));

        // Add size progress component (Size column)
        model.Add(DownloadColumns.Size.ComponentKey, new DownloadComponents.SizeProgressComponent(
            initialDownloaded: download.DownloadedBytes,
            initialTotal: download.FileSize,
            downloadedObservable: download.WhenAnyValue(x => x.DownloadedBytes).ToObservable(),
            totalObservable: download.WhenAnyValue(x => x.FileSize).ToObservable()));

        // Add speed component (Speed column)
        model.Add(DownloadColumns.Speed.ComponentKey, new DownloadComponents.SpeedComponent(
            initialTransferRate: download.TransferRate,
            transferRateObservable: download.WhenAnyValue(x => x.TransferRate).ToObservable()));

        // Add status component with embedded actions (Status column)
        model.Add(DownloadColumns.Status.ComponentKey, new DownloadComponents.StatusComponent(
            downloadsService: _downloadsService,
            downloadInfo: download,
            initialProgress: download.Progress,
            initialStatus: download.Status,
            progressObservable: download.WhenAnyValue(x => x.Progress).ToObservable(),
            statusObservable: download.WhenAnyValue(x => x.Status).ToObservable()));

        // Add download reference component
        model.Add(DownloadColumns.DownloadRefComponentKey, new DownloadRef(download));

        return model;
    }

    private static string ResolveGameNameInitial(GameId gameId)
    {
        // Return a default value since we can't access the registry synchronously
        return "Unknown Game";
    }

    private Observable<string> ResolveGameNameObservable(GameId gameId)
    {
        return _gameRegistry.InstalledGames
            .ToObservableChangeSet()
            .Transform(game => game.Game)
            .Filter(game => game.GameId.Equals(gameId))
            .QueryWhenChanged(query => query.FirstOrDefault()?.Name ?? "Unknown Game")
            .ToObservable()
            .Prepend("Unknown Game");
    }

    private ImageComponent CreateIconComponent(DownloadInfo download)
    {
        try
        {
            // Try to load FileMetadata from the database
            var fileMetadata = NexusModsFileMetadata.Load(_connection.Db, download.FileMetadataId);
            
            // Check if the loaded metadata is valid
            if (fileMetadata.IsValid())
            {
                // Use the thumbnail loader with the ModPage ID
                return ImageComponent.FromPipeline(_thumbnailLoader.Value, fileMetadata.ModPageId, ImagePipelines.ModPageThumbnailFallback);
            }
        }
        catch
        {
            // Fall through to fallback
        }

        // Return fallback image component if metadata cannot be loaded or is invalid
        return new ImageComponent(ImagePipelines.ModPageThumbnailFallback);
    }


}
