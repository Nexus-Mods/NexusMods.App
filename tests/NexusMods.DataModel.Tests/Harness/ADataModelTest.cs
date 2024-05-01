using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.DataModel.Loadouts;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;
using Xunit.DependencyInjection;
using DownloadId = NexusMods.Abstractions.FileStore.Downloads.DownloadId;
using Entity = NexusMods.MnemonicDB.Abstractions.Models.Entity;
using IGame = NexusMods.Abstractions.Games.IGame;

// ReSharper disable StaticMemberInGenericType

namespace NexusMods.DataModel.Tests.Harness;

public abstract class ADataModelTest<T> : IDisposable, IAsyncLifetime
{
    public AbsolutePath DataZipLzma => FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("Resources/data_zip_lzma.zip");
    public Hash DataZipLzmaHash => Hash.From(0x706F72D12A82892D);
    public AbsolutePath DataZipLzmaWithExtraFile => FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("Resources/data_zip_lzma_withextraFile.zip");
    public AbsolutePath Data7ZLzma2 => FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("Resources/data_7zip_lzma2.7z");

    protected readonly TemporaryFileManager TemporaryFileManager;
    protected readonly IServiceProvider ServiceProvider;
    protected readonly IFileStore FileStore;
    protected readonly IArchiveInstaller ArchiveInstaller;

    protected readonly IApplyService ApplyService;
    protected readonly FileHashCache FileHashCache;
    protected readonly IFileSystem FileSystem;
    protected readonly IDataStore DataStore;
    protected readonly IConnection Connection;
    protected readonly IFileOriginRegistry FileOriginRegistry;
    protected readonly DiskStateRegistry DiskStateRegistry;
    protected readonly IToolManager ToolManager;
    protected readonly IGameRegistry GameRegistry;

    protected readonly IGame Game;
    protected readonly GameInstallation Install;
    
    protected Loadout.Model BaseLoadout = null!;

    protected CancellationToken Token = CancellationToken.None;
    private readonly IHost _host;
    protected readonly ILogger<T> Logger;

    protected ADataModelTest(IServiceProvider _)
    {
        var host = new HostBuilder();
        host.ConfigureServices(services =>
        {
            services.AddSingleton<ITestOutputHelperAccessor>
                (s => _.GetRequiredService<ITestOutputHelperAccessor>());
            Startup.AddServices(services);
            
        });
        
        _host = host.Build();
        var provider = _host.Services;
        FileStore = provider.GetRequiredService<IFileStore>();
        ArchiveInstaller = provider.GetRequiredService<IArchiveInstaller>();
        ApplyService = provider.GetRequiredService<IApplyService>();
        FileHashCache = provider.GetRequiredService<FileHashCache>();
        FileSystem = provider.GetRequiredService<IFileSystem>();
        DataStore = provider.GetRequiredService<IDataStore>();
        Connection = provider.GetRequiredService<IConnection>();
        FileOriginRegistry = provider.GetRequiredService<IFileOriginRegistry>();
        DiskStateRegistry = provider.GetRequiredService<DiskStateRegistry>();
        Logger = provider.GetRequiredService<ILogger<T>>();
        TemporaryFileManager = provider.GetRequiredService<TemporaryFileManager>();
        ToolManager = provider.GetRequiredService<IToolManager>();
        ServiceProvider = provider;
        GameRegistry = provider.GetRequiredService<IGameRegistry>();

        Install = GameRegistry.AllInstalledGames.First(g => g.Game is StubbedGame);
        Game = (IGame)Install.Game;
    }

    public void Dispose()
    {
        _host.Dispose();
    }

    public virtual async Task InitializeAsync()
    {
        BaseLoadout = await Game.Synchronizer.Manage(Install, "TestLoadout_" + Guid.NewGuid());
    }

    /// <summary>
    /// "Primes" the test filesystem with the given file. This means that the file is copied to the test (in-memory)
    /// filesystem so it can be used in tests.
    /// </summary>
    private async Task PrimeFile(AbsolutePath src)
    {
        {
            await using var file = FileSystem.CreateFile(src);
            await using var stream = src.Read();
            await stream.CopyToAsync(file, Token);
        }
        var entry = FileSystem.GetFileEntry(src);
        entry.LastWriteTime = DateTime.UtcNow;
        entry.CreationTime = DateTime.UtcNow;

        return;
    }

    protected async Task<ModId[]> AddMods(LoadoutId loadoutId, AbsolutePath path, string? name = null)
    {
        var downloadId = await FileOriginRegistry.RegisterDownload(path, Token);
        var result = await ArchiveInstaller.AddMods(loadoutId, downloadId, name, token: Token);
        // Refresh the loadout to get the new mods, as a convenience.
        Refresh(ref BaseLoadout);
        return result;
    }

    protected Task<ModId[]> AddMods(Loadout.Model loadout, AbsolutePath path, string? name = null)
    {
        return AddMods(LoadoutId.From(loadout.Id), path, name);
    }

    /// <summary>
    /// Creates a download from the given files, and data (saved as UTF-8 strings), and registers it with the FileOriginRegistry,
    /// returning the download id.
    /// </summary>
    /// <param name="files"></param>
    /// <returns></returns>
    protected async Task<DownloadId> RegisterDownload(params (string Name, string Data)[] files)
    {
        await using var tmpFile = TemporaryFileManager.CreateFile(KnownExtensions.Zip);
        using var memoryStream = new MemoryStream();
        using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var (name, data) in files)
            {
                var entry = zip.CreateEntry(name, CompressionLevel.Fastest);
                await using var stream = entry.Open();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(data);
            }
        }

        memoryStream.Position = 0;
        await using (var fileStream = tmpFile.Path.Create())
        {
            await memoryStream.CopyToAsync(fileStream, Token);
        }

        return await FileOriginRegistry.RegisterDownload(tmpFile.Path, Token);
    }

    /// <summary>
    /// Adds a mod to the given loadout, with the given files (saved as UTF-8 strings), and returns the mod id.
    /// </summary>
    protected async Task<ModId> AddMod(string modName, params (string Name, string Data)[] files)
    {
        var downloadId = await RegisterDownload(files);
        var modIds = await ArchiveInstaller.AddMods(LoadoutId.From(BaseLoadout.Id), downloadId, modName, token: Token);
        return modIds.First();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears data store of content from previous runs.
    /// Only valid if tests are ran non-concurrently.
    /// </summary>
    private void ClearDataStore()
    {
        // TODO: Replace this with something more performant.
        //       This is not being done now as we'll be switching from SQLite to RocksDB with EventSourcing
        //       , therefore code will change there.
        foreach (var category in Enum.GetValues<EntityCategory>())
        foreach (var id in DataStore.AllIds(category))
            DataStore.Delete(id);
    }

    public void Refresh<T>(ref T ent) where T : Entity
    {
        ent = Connection.Db.Get<T>(ent.Id);
    }
}
