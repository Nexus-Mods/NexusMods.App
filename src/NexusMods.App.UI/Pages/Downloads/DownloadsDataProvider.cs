using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Resources;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.NexusModsApi;
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
    private readonly ILogger? _logger = serviceProvider.GetService<ILogger>();


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
        model.Add(DownloadColumns.Name.NameComponentKey, new NameComponent(
            initialValue: download.Name.Value,
            valueObservable: download.Name.AsObservable()));

        // Add icon component for Name+Icon column
        model.Add(DownloadColumns.Name.ImageComponentKey, CreateIconComponent(download));

        // Add game component
        model.Add(DownloadColumns.Game.ComponentKey, new DownloadComponents.GameComponent(
            gameName: ResolveGameName(download.GameId.Value)));

        // Add size progress component (Size column)
        model.Add(DownloadColumns.Size.ComponentKey, new DownloadComponents.SizeProgressComponent(
            initialDownloaded: download.DownloadedBytes.Value,
            initialTotal: download.FileSize.Value,
            downloadedObservable: download.DownloadedBytes.AsObservable(),
            totalObservable: download.FileSize.AsObservable()));

        // Add speed component (Speed column)
        model.Add(DownloadColumns.Speed.ComponentKey, new DownloadComponents.SpeedComponent(
            initialTransferRate: download.TransferRate.Value,
            transferRateObservable: download.TransferRate.AsObservable()));

        // Add status component with embedded actions (Status column)
        model.Add(DownloadColumns.Status.ComponentKey, new DownloadComponents.StatusComponent(
            initialProgress: download.Progress.Value,
            initialStatus: download.Status.Value,
            progressObservable: download.Progress.AsObservable(),
            statusObservable: download.Status.AsObservable()));

        // Add download reference component
        model.Add(DownloadColumns.DownloadRefComponentKey, new DownloadRef(download));

        return model;
    }

    public string ResolveGameName(NexusModsGameId nexusModsGameId)
    {
        return _gameRegistry.LocateGameInstallations()
            .FirstOrDefault(g => g.Game.NexusModsGameId.Equals(nexusModsGameId))?.Game.DisplayName 
            ?? Language.Downloads_UnknownGame;
    }

    private ImageComponent CreateIconComponent(DownloadInfo download)
    {
        try
        {
            // Try to load FileMetadata from the database
            var fileMetadata = NexusModsFileMetadata.Load(_connection.Db, download.FileMetadataId.Value);
            
            // Check if the loaded metadata is valid
            if (fileMetadata.IsValid())
            {
                // Use the thumbnail loader with the ModPage ID
                return ImageComponent.FromPipeline(_thumbnailLoader.Value, fileMetadata.ModPageId, ImagePipelines.ModPageThumbnailFallback);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load thumbnail for download {DownloadId}", download.Id);
            // Fall through to fallback
        }

        // Return fallback image component if metadata cannot be loaded or is invalid
        return new ImageComponent(ImagePipelines.ModPageThumbnailFallback);
    }


}
