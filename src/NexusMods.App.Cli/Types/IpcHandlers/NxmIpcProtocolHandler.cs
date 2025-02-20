using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.EventBus;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GOG;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.Auth;
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
    private readonly IClient _client;
    private readonly IEventBus _eventBus;

    /// <summary>
    /// constructor
    /// </summary>
    public NxmIpcProtocolHandler(
        IServiceProvider serviceProvider,
        ILogger<NxmIpcProtocolHandler> logger, 
        OAuth oauth,
        IClient client,
        IGameDomainToGameIdMappingCache cache,
        ILoginManager loginManager)
    {
        _serviceProvider = serviceProvider;
        _eventBus = serviceProvider.GetRequiredService<IEventBus>();

        _logger = logger;
        _oauth = oauth;
        _client = client;
        _cache = cache;
        _loginManager = loginManager;
    }

    /// <inheritdoc/>
    public async Task Handle(string url, CancellationToken cancel)
    {
        var parsed = NXMUrl.Parse(url);

        // NOTE(erri120): don't log OAuth callbacks, they contain sensitive information
        if (parsed is not NXMOAuthUrl && parsed is not NXMGogAuthUrl) _logger.LogDebug("Received NXM URL: {Url}", parsed.ToString());
        else _logger.LogDebug("Received URL of type {Type}", parsed.GetType());

        var userInfo = await _loginManager.GetUserInfoAsync(cancel);
        switch (parsed)
        {
            case NXMOAuthUrl oauthUrl:
                _oauth.AddUrl(oauthUrl);
                break;
            case NXMGogAuthUrl gogUrl:
                _client.AuthUrl(gogUrl);
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
        var syncService = _serviceProvider.GetRequiredService<ISynchronizerService>();

        GameInstallation? game = null;
        Loadout.ReadOnly loadout = default;
        foreach (var installedGame in gameRegistry.InstalledGames)
        {
            if (installedGame.Game.GameId == gameId)
            {
                if (syncService.TryGetLastAppliedLoadout(installedGame, out loadout))
                {
                    game = installedGame;
                    break;
                }
            }
        }
        if (game is null)
        {
            _logger.LogError("No game {Game} installed with an active loadout", collectionUrl.Game);
            return;
        }
                    
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
        _eventBus.Send(new CliMessages.AddedCollection(collectionRevision));
    }

    private async Task HandleModUrl(CancellationToken cancel, NXMModUrl modUrl)
    {
        var nexusModsLibrary = _serviceProvider.GetRequiredService<NexusModsLibrary>();

        var alreadyDownloaded = await nexusModsLibrary.IsAlreadyDownloaded(modUrl, cancellationToken: cancel);
        if (alreadyDownloaded)
        {
            _logger.LogInformation("File `{Game}/{ModId}/{FileId}` has already been downloaded and will be skipped", modUrl.Game, modUrl.ModId, modUrl.FileId);
            return;
        }

        var library = _serviceProvider.GetRequiredService<ILibraryService>();
        var temporaryFileManager = _serviceProvider.GetRequiredService<TemporaryFileManager>();

        _eventBus.Send(new CliMessages.AddedDownload());

        await using var destination = temporaryFileManager.CreateFile();
        var downloadJob = await nexusModsLibrary.CreateDownloadJob(destination, modUrl, cancellationToken: cancel);

        var libraryJob = await library.AddDownload(downloadJob);
    }
}

