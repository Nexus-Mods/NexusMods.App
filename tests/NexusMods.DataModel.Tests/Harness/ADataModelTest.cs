using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;
using Xunit.DependencyInjection;
// ReSharper disable StaticMemberInGenericType

namespace NexusMods.DataModel.Tests.Harness;

public abstract class ADataModelTest<T> : IDisposable, IAsyncLifetime
{
    public AbsolutePath DataZipLzma => FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("Resources/data_zip_lzma.zip");
    public AbsolutePath Data7ZLzma2 => FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("Resources/data_7zip_lzma2.7z");

    public AbsolutePath DataTest =>
        FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("Resources/data.test");

    public static readonly RelativePath[] DataNames = new[]
    {
        "rootFile.txt",
        "folder1/folder1file.txt",
        "deepFolder/deepFolder2/deepFolder3/deepFolder4/deepFile.txt"
    }.Select(t => t.ToRelativePath()).ToArray();

    public static readonly Dictionary<RelativePath, (Hash Hash, Size Size)> DataContents = DataNames
        .ToDictionary(d => d,
            d => (d.FileName.ToString().XxHash64AsUtf8(), Size.FromLong(d.FileName.ToString().Length)));

    protected readonly TemporaryFileManager TemporaryFileManager;
    protected readonly IServiceProvider ServiceProvider;
    protected readonly IArchiveAnalyzer ArchiveAnalyzer;
    protected readonly IArchiveManager ArchiveManager;
    protected readonly IArchiveInstaller ArchiveInstaller;
    protected readonly LoadoutManager LoadoutManager;
    protected readonly LoadoutSynchronizer LoadoutSynchronizer;
    protected readonly FileHashCache FileHashCache;
    protected readonly IFileSystem FileSystem;
    protected readonly IDataStore DataStore;
    protected readonly IToolManager ToolManager;

    protected readonly IGame Game;
    protected readonly GameInstallation Install;
    protected LoadoutMarker BaseList; // set via InitializeAsync
    protected readonly ILogger<T> Logger;
    private readonly IHost _host;

    protected CancellationToken Token = CancellationToken.None;

    protected ADataModelTest(IServiceProvider provider)
    {
        var startup = new Startup();
        _host = Host.CreateDefaultBuilder(Environment.GetCommandLineArgs())
            .ConfigureServices((_, service) => startup.ConfigureServices(service))
            .Build();
        var provider1 = _host.Services;
        ArchiveAnalyzer = provider1.GetRequiredService<IArchiveAnalyzer>();
        ArchiveManager = provider1.GetRequiredService<IArchiveManager>();
        ArchiveInstaller = provider1.GetRequiredService<IArchiveInstaller>();
        LoadoutManager = provider1.GetRequiredService<LoadoutManager>();
        FileHashCache = provider1.GetRequiredService<FileHashCache>();
        FileSystem = provider1.GetRequiredService<IFileSystem>();
        DataStore = provider1.GetRequiredService<IDataStore>();
        Logger = provider1.GetRequiredService<ILogger<T>>();
        LoadoutSynchronizer = provider1.GetRequiredService<LoadoutSynchronizer>();
        TemporaryFileManager = provider1.GetRequiredService<TemporaryFileManager>();
        ToolManager = provider1.GetRequiredService<IToolManager>();
        ServiceProvider = provider;

        Game = provider1.GetRequiredService<StubbedGame>();
        Install = Game.Installations.First();

        startup.Configure(provider1.GetRequiredService<ILoggerFactory>(), provider.GetRequiredService<ITestOutputHelperAccessor>());

    }

    public void Dispose()
    {
        _host.Dispose();
    }

    public async Task InitializeAsync()
    {
        BaseList = await LoadoutManager.ManageGameAsync(Install, "BaseList", CancellationToken.None);
    }


    protected async Task<ModId[]> AddMods(LoadoutMarker mainList, AbsolutePath path, string? name = null)
    {
        var hash1 = await ArchiveAnalyzer.AnalyzeFileAsync(path, CancellationToken.None);
        return await ArchiveInstaller.AddMods(mainList.Value.LoadoutId, hash1.Hash, name, CancellationToken.None);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
