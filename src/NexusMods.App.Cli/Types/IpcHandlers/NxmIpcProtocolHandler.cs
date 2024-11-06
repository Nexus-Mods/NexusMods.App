using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Collections;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.Auth;
using NexusMods.Networking.NexusWebApi.V1Interop;
using NexusMods.Paths;

namespace NexusMods.CLI.Types.IpcHandlers;

/// <summary>
/// a handler for nxm:// urls
/// </summary>
// ReSharper disable once InconsistentNaming
public class NxmIpcProtocolHandler : IIpcProtocolHandler
{
    /// <inheritdoc/>
    public string Protocol => "nxm";

    private readonly ILogger<NxmIpcProtocolHandler> _logger;
    private readonly ILoginManager _loginManager;
    private readonly OAuth _oauth;
    private readonly IGameDomainToGameIdMappingCache _cache;

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// constructor
    /// </summary>
    public NxmIpcProtocolHandler(
        IServiceProvider serviceProvider,
        ILogger<NxmIpcProtocolHandler> logger, 
        OAuth oauth,
        IGameDomainToGameIdMappingCache cache,
        ILoginManager loginManager)
    {
        _serviceProvider = serviceProvider;

        _logger = logger;
        _oauth = oauth;
        _cache = cache;
        _loginManager = loginManager;
    }

    /// <inheritdoc/>
    public async Task Handle(string url, CancellationToken cancel)
    {
        var parsed = NXMUrl.Parse(url);
        _logger.LogDebug("Received NXM URL: {Url}", parsed);
        var userInfo = await _loginManager.GetUserInfoAsync(cancel);
        switch (parsed)
        {
            case NXMOAuthUrl oauthUrl:
                _oauth.AddUrl(oauthUrl);
                break;
            case NXMModUrl modUrl:
                // Check if the user is logged in
                if (userInfo is not null)
                {
                    await HandleModUrl(cancel, modUrl);
                }
                else
                {
                    _logger.LogWarning("Download failed: User is not logged in");
                }
                break;
            case NXMCollectionUrl collectionUrl:
                // Check if the user is logged in
                if (userInfo is not null)
                {
                    await HandleCollectionUrl(collectionUrl);
                }
                else
                {
                    _logger.LogWarning("Download failed: User is not logged in");
                }
                break;
            default:
                _logger.LogWarning("Unknown NXM URL type: {Url}", parsed);
                break;
        }
    }

    private async Task HandleCollectionUrl(NXMCollectionUrl collectionUrl)
    {
        var domain = GameDomain.From(collectionUrl.Game);
        var gameId = (await _cache.TryGetIdAsync(domain, CancellationToken.None)).Value.Value;
        var nexusModsLibrary = _serviceProvider.GetRequiredService<NexusModsLibrary>();
        var library = _serviceProvider.GetRequiredService<ILibraryService>();
        var gameRegistry = _serviceProvider.GetRequiredService<IGameRegistry>();
        var connection = _serviceProvider.GetRequiredService<IConnection>();
        if (!gameRegistry.InstalledGames.TryGetFirst(g => g.Game.GameId == gameId, out var game))
            return;
                    
        var syncService = _serviceProvider.GetRequiredService<ISynchronizerService>();
        if (!syncService.TryGetLastAppliedLoadout(game, out var loadout))
            return;
                    
        var temporaryFileManager = _serviceProvider.GetRequiredService<TemporaryFileManager>();
        await using var destination = temporaryFileManager.CreateFile();

        var slug = collectionUrl.Collection.Slug;
        var revision = collectionUrl.Revision;

        var db = connection.Db;
        var list = db.Datoms(
            (NexusModsCollectionLibraryFile.CollectionSlug, slug),
            (NexusModsCollectionLibraryFile.CollectionRevisionNumber, revision)
        );

        if (!list.Select(id => NexusModsCollectionLibraryFile.Load(db, id)).TryGetFirst(x => x.IsValid(), out var collectionFile))
        {
            var downloadJob = nexusModsLibrary.CreateCollectionDownloadJob(destination, collectionUrl.Collection.Slug, collectionUrl.Revision, CancellationToken.None);
            var libraryFile = await library.AddDownload(downloadJob);

            if (!libraryFile.TryGetAsNexusModsCollectionLibraryFile(out collectionFile))
                throw new InvalidOperationException("The library file is not a NexusModsCollectionLibraryFile");
        }

        var collectionRevision = await nexusModsLibrary.GetOrAddCollectionRevision(collectionFile, collectionUrl.Collection.Slug, collectionUrl.Revision, CancellationToken.None);
        // var installJob = await InstallCollectionJob.Create(_serviceProvider, loadout, collectionFile);
    }

    private async Task HandleModUrl(CancellationToken cancel, NXMModUrl modUrl)
    {
        var nexusModsLibrary = _serviceProvider.GetRequiredService<NexusModsLibrary>();
        var library = _serviceProvider.GetRequiredService<ILibraryService>();
        var temporaryFileManager = _serviceProvider.GetRequiredService<TemporaryFileManager>();
        var cache = _serviceProvider.GetRequiredService<IGameDomainToGameIdMappingCache>();

        await using var destination = temporaryFileManager.CreateFile();
        var downloadJob = await nexusModsLibrary.CreateDownloadJob(destination, modUrl, cache, cancellationToken: cancel);

        var libraryJob = await library.AddDownload(downloadJob);
        _logger.LogInformation("{Result}", libraryJob);

        // var task = await _downloadService.AddTask(modUrl);
        // _ = task.StartAsync();
    }
}

