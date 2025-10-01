using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Sdk.EventBus;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GOG;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.Auth;
using NexusMods.Paths;
using NexusMods.Sdk;
using System.Threading.Tasks;

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

        var isUserLogged = await _loginManager.GetIsUserLoggedInAsync(cancel);
        switch (parsed)
        {
            case NXMOAuthUrl oauthUrl:
                _oauth.AddUrl(oauthUrl);
                break;
            case NXMGogAuthUrl gogUrl:
                _client.AuthUrl(gogUrl);
                break;
            case NXMProtocolRegistrationCheck protocolRegistrationTest:
                _eventBus.Send(new CliMessages.TestProtocolRegistration(protocolRegistrationTest.Id));
                break;
            case NXMModUrl modUrl:
                await HandleModUrl(modUrl, cancel);
                break;
            case NXMCollectionUrl collectionUrl:
                await HandleCollectionUrl(collectionUrl, cancel);
                break;
            default:
                _logger.LogWarning("Unknown NXM URL type: {Url}", parsed);
                break;
        }
    }

    private async Task HandleCollectionUrl(NXMCollectionUrl collectionUrl, CancellationToken cancel)
    {
        var isUserLogged = await _loginManager.GetIsUserLoggedInAsync(cancel);
        if (!isUserLogged)
        {
            _logger.LogWarning("Download failed: User is not logged in");
            _eventBus.Send(new CliMessages.CollectionAddFailed(new FailureReason.NotLoggedIn()));
            return;
        }
        
        var domain = GameDomain.From(collectionUrl.Game);
        var nexusModsLibrary = _serviceProvider.GetRequiredService<NexusModsLibrary>();
        var library = _serviceProvider.GetRequiredService<ILibraryService>();
        var connection = _serviceProvider.GetRequiredService<IConnection>();

        var game = GetManagedGameFor(domain);
        if (game is null)
        {
            _logger.LogWarning("Collection add aborted: {Game} is not a managed game", collectionUrl.Game);
            _eventBus.Send(new CliMessages.CollectionAddFailed(new FailureReason.GameNotManaged(collectionUrl.Game)));
            return;
        }
                    
        var temporaryFileManager = _serviceProvider.GetRequiredService<TemporaryFileManager>();
        _eventBus.Send(new CliMessages.CollectionAddStarted());

        try
        {
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
            
            _eventBus.Send(new CliMessages.CollectionAddSucceeded(collectionRevision));
        }
        catch (Exception e)
        {
            _eventBus.Send(new CliMessages.CollectionAddFailed(new FailureReason.Unknown(e)));
            throw;
        }
    }

    private async Task HandleModUrl(NXMModUrl modUrl, CancellationToken cancel)
    {
        var isUserLogged = await _loginManager.GetIsUserLoggedInAsync(cancel);
        if (!isUserLogged)
        {
            _logger.LogWarning("Download failed: User is not logged in");
            _eventBus.Send(new CliMessages.ModDownloadFailed(new FailureReason.NotLoggedIn()));
            return;
        }
        
        var nexusModsLibrary = _serviceProvider.GetRequiredService<NexusModsLibrary>();

        var (alreadyDownloaded, items) = await nexusModsLibrary.IsAlreadyDownloaded(modUrl, cancellationToken: cancel);
        if (alreadyDownloaded)
        {
            _logger.LogInformation("File `{Game}/{ModId}/{FileId}` has already been downloaded and will be skipped", modUrl.Game, modUrl.ModId, modUrl.FileId);
            _eventBus.Send(new CliMessages.ModDownloadFailed(new FailureReason.AlreadyExists(items.First().AsLibraryItem().Name)));
            return;
        }
        
        var domain = GameDomain.From(modUrl.Game);
        var game = GetManagedGameFor(domain);
        if (game is null)
        {
            _logger.LogWarning("Mod download aborted: {Game} is not a managed game", modUrl.Game);
            _eventBus.Send(new CliMessages.ModDownloadFailed(new FailureReason.GameNotManaged(modUrl.Game)));
            return;
        }

        var library = _serviceProvider.GetRequiredService<ILibraryService>();
        var temporaryFileManager = _serviceProvider.GetRequiredService<TemporaryFileManager>();

        _eventBus.Send(new CliMessages.ModDownloadStarted());
        
        LibraryFile.ReadOnly? libraryFile = null;
        try
        {
            await using var destination = temporaryFileManager.CreateFile();
            var downloadJob = await nexusModsLibrary.CreateDownloadJob(destination, modUrl, cancellationToken: cancel);

            libraryFile = await library.AddDownload(downloadJob);
            
            _eventBus.Send(new CliMessages.ModDownloadSucceeded(libraryFile.Value.AsLibraryItem()));
        }
        catch (TaskCanceledException)
        {
            // User-initiated cancellation should not be treated as an error
            _logger.LogInformation("Mod download cancelled by user for {Game}/{ModId}/{FileId}", modUrl.Game, modUrl.ModId, modUrl.FileId);
            // Don't rethrow TaskCanceledException for user-initiated cancellations
            // Don't send ModDownloadFailed event for user-initiated cancellations
        }
        catch (Exception e)
        {
            _eventBus.Send(new CliMessages.ModDownloadFailed(new FailureReason.Unknown(e)));
            throw;
        }
    }
    
    
    private GameInstallation? GetManagedGameFor(GameDomain domain)
    {
        var gameRegistry = _serviceProvider.GetRequiredService<IGameRegistry>();
        var connection = _serviceProvider.GetRequiredService<IConnection>();
        var syncService = _serviceProvider.GetRequiredService<ISynchronizerService>();
        
        var gameId = _cache[domain];
        foreach (var installedGame in gameRegistry.InstalledGames)
        {
            if (installedGame.Game.GameId != gameId) continue;
            
            if (syncService.TryGetLastAppliedLoadout(installedGame, out _))
                return installedGame;

            var activeLoadouts = Loadout.All(connection.Db)
                .Where(ld => ld.InstallationInstance.Game.GameId == installedGame.Game.GameId);

            if (!activeLoadouts.Any()) continue;
            
            return installedGame;
        }
        
        return null;
    }
}

