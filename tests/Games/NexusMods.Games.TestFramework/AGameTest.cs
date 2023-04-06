using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;
using ModId = NexusMods.Networking.NexusWebApi.Types.ModId;

namespace NexusMods.Games.TestFramework;

[PublicAPI]
public abstract class AGameTest<TGame> where TGame : AGame
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly TGame Game;
    protected readonly GameInstallation GameInstallation;

    protected readonly TemporaryFileManager TemporaryFileManager;
    protected readonly LoadoutManager LoadoutManager;
    protected readonly IDataStore DataStore;

    protected readonly Client NexusClient;
    protected readonly IHttpDownloader HttpDownloader;

    protected AGameTest(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        Game = serviceProvider.FindImplementationInContainer<TGame, IGame>();
        GameInstallation = Game.Installations.First();

        TemporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
        LoadoutManager = serviceProvider.GetRequiredService<LoadoutManager>();
        DataStore = serviceProvider.GetRequiredService<IDataStore>();

        NexusClient = serviceProvider.GetRequiredService<Client>();
        HttpDownloader = serviceProvider.GetRequiredService<IHttpDownloader>();
    }

    /// <summary>
    /// Creates a new loadout and returns the <see cref="LoadoutMarker"/> of it.
    /// </summary>
    /// <returns></returns>
    protected async Task<LoadoutMarker> CreateLoadout()
    {
        var loadout = await LoadoutManager.ManageGameAsync(GameInstallation, Guid.NewGuid().ToString("N"));
        return loadout;
    }

    /// <summary>
    /// Downloads a mod and returns the <see cref="TemporaryPath"/> and <see cref="Hash"/> of it.
    /// </summary>
    /// <param name="gameDomain"></param>
    /// <param name="modId"></param>
    /// <param name="fileId"></param>
    /// <returns></returns>
    protected async Task<(TemporaryPath file, Hash downloadHash)> DownloadModAsync(GameDomain gameDomain, ModId modId, FileId fileId)
    {
        var links = await NexusClient.DownloadLinks(gameDomain, modId, fileId);
        var file = TemporaryFileManager.CreateFile();

        var downloadHash = await HttpDownloader.DownloadAsync(
            links.Data.Select(u => new HttpRequestMessage(HttpMethod.Get, u.Uri)).ToArray(),
            file
        );

        return (file, downloadHash);
    }

    /// <summary>
    /// Installs a mod into the loadout and returns the <see cref="Mod"/> of it.
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="path"></param>
    /// <param name="modName"></param>
    /// <returns></returns>
    protected async Task<Mod> InstallModIntoLoadoutAsync(LoadoutMarker loadout, AbsolutePath path, string? modName = null)
    {
        var modId = await loadout.InstallModAsync(path, modName ?? Guid.NewGuid().ToString("N"));
        return loadout.Value.Mods[modId];
    }
}
