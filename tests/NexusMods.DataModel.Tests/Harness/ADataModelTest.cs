using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.DataModel.Loadouts;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;
using Xunit.DependencyInjection;
using DownloadId = NexusMods.Abstractions.FileStore.Downloads.DownloadId;
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
    protected readonly ILibraryItemInstaller LibraryItemInstaller;

    protected readonly IApplyService ApplyService;
    protected readonly FileHashCache FileHashCache;
    protected readonly IFileSystem FileSystem;
    protected readonly IConnection Connection;
    protected readonly ILibraryService LibraryService;
    protected readonly DiskStateRegistry DiskStateRegistry;
    protected readonly IToolManager ToolManager;
    protected readonly IGameRegistry GameRegistry;
    protected ILoadoutSynchronizer Synchronizer;

    protected IGame Game;
    protected GameInstallation Install;
    
    protected Loadout.ReadOnly BaseLoadout;

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
        LibraryItemInstaller = provider.GetRequiredService<ILibraryItemInstaller>();
        ApplyService = provider.GetRequiredService<IApplyService>();
        FileHashCache = provider.GetRequiredService<FileHashCache>();
        FileSystem = provider.GetRequiredService<IFileSystem>();
        Connection = provider.GetRequiredService<IConnection>();
        LibraryService = provider.GetRequiredService<ILibraryService>();
        DiskStateRegistry = provider.GetRequiredService<DiskStateRegistry>();
        Logger = provider.GetRequiredService<ILogger<T>>();
        TemporaryFileManager = provider.GetRequiredService<TemporaryFileManager>();
        ToolManager = provider.GetRequiredService<IToolManager>();
        ServiceProvider = provider;
        GameRegistry = provider.GetRequiredService<IGameRegistry>();
    }

    public void Dispose()
    {
        _host.Dispose();
    }

    public virtual async Task InitializeAsync()
    {
        await _host.StartAsync(Token);
        Install = await StubbedGame.Create(ServiceProvider);
        Game = (IGame)Install.Game;
        Synchronizer = Game.Synchronizer;
        BaseLoadout = await Synchronizer.CreateLoadout(Install, "TestLoadout_" + Guid.NewGuid());
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

    protected async Task<LoadoutItemGroup.ReadOnly[]> AddMods(LoadoutId loadoutId, AbsolutePath path, string? name = null)
    {
        var job = LibraryService.AddLocalFile(path);
        await job.StartAsync(Token);
        var libraryResult = await job.WaitToFinishAsync(Token);
        
        if (!libraryResult.TryGetCompleted(out var completed))
            throw new Exception("Failed to add mod to library");

        if (!completed.TryGetData<LocalFile.ReadOnly>(out var localFile))
            throw new Exception("Failed to add mod to library");
        
        using var tx = Connection.BeginTransaction();
        var newGroups = await LibraryItemInstaller.ExecuteAsync(localFile.AsLibraryFile().AsLibraryItem(), tx, Loadout.Load(Connection.Db, loadoutId), Token); 
        // Refresh the loadout to get the new mods, as a convenience.
        
        var result = await tx.Commit();
        
        return newGroups.Select(group => result.Remap(group)).OfTypeLoadoutItemGroup().ToArray();
    }

    protected Task<LoadoutItemGroup.ReadOnly[]> AddMods(Loadout.ReadOnly loadout, AbsolutePath path, string? name = null)
    {
        return AddMods(LoadoutId.From(loadout.Id), path, name);
    }

    
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public void Refresh<T>(ref T ent) where T : IReadOnlyModel<T>
    {
        ent = T.Create(Connection.Db, ent.Id);
    }
}
