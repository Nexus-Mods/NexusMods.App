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
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection;

namespace NexusMods.DataModel.Tests.Harness;

public abstract class ADataModelTest<T> : IDisposable, IAsyncLifetime
{
    public static readonly AbsolutePath DATA_ZIP_LZMA = KnownFolders.EntryFolder.CombineUnchecked(@"Resources\data_zip_lzma.zip");
    public static readonly AbsolutePath DATA_7Z_LZMA2 = KnownFolders.EntryFolder.CombineUnchecked(@"Resources\data_7zip_lzma2.7z");

    public static readonly RelativePath[] DATA_NAMES = new[]
    {
        "rootFile.txt",
        "folder1/folder1file.txt",
        "deepFolder/deepFolder2/deepFolder3/deepFolder4/deepFile.txt"
    }.Select(t => t.ToRelativePath()).ToArray();

    public static readonly Dictionary<RelativePath, (Hash Hash, Size Size)> DATA_CONTENTS = DATA_NAMES
        .ToDictionary(d => d,
            d => (d.FileName.ToString().XxHash64(), Size.From(d.FileName.ToString().Length)));

    private readonly IServiceProvider _provider;
    protected readonly TemporaryFileManager TemporaryFileManager;
    protected readonly IServiceProvider ServiceProvider;
    protected readonly FileContentsCache ArchiveContentsCache;
    protected readonly ArchiveManager ArchiveManager;
    protected readonly LoadoutManager LoadoutManager;
    protected readonly FileHashCache FileHashCache;
    protected readonly IDataStore DataStore;

    protected readonly IGame Game;
    protected readonly GameInstallation Install;
    protected LoadoutMarker? BaseList;
    protected readonly ILogger<T> _logger;
    private readonly IHost _host;

    protected CancellationToken Token = CancellationToken.None;

    protected ADataModelTest(IServiceProvider provider)
    {
        var startup = new Startup();
        _host = Host.CreateDefaultBuilder(Environment.GetCommandLineArgs())
            .ConfigureServices((_, service) => startup.ConfigureServices(service))
            .Build();
        _provider = _host.Services;
        ArchiveContentsCache = _provider.GetRequiredService<FileContentsCache>();
        ArchiveManager = _provider.GetRequiredService<ArchiveManager>();
        LoadoutManager = _provider.GetRequiredService<LoadoutManager>();
        FileHashCache = _provider.GetRequiredService<FileHashCache>();
        DataStore = _provider.GetRequiredService<IDataStore>();
        _logger = _provider.GetRequiredService<ILogger<T>>();
        TemporaryFileManager = _provider.GetRequiredService<TemporaryFileManager>();
        ServiceProvider = provider;

        Game = _provider.GetRequiredService<StubbedGame>();
        Install = Game.Installations.First();

        startup.Configure(_provider.GetRequiredService<ILoggerFactory>(), provider.GetRequiredService<ITestOutputHelperAccessor>());

    }


    public void Dispose()
    {
        _host.Dispose();
    }

    public async Task InitializeAsync()
    {
        await ArchiveContentsCache.AnalyzeFile(DATA_ZIP_LZMA, Token);
        await ArchiveContentsCache.AnalyzeFile(DATA_7Z_LZMA2, Token);
        await ArchiveManager.ArchiveFile(DATA_ZIP_LZMA, Token);
        await ArchiveManager.ArchiveFile(DATA_7Z_LZMA2, Token);

        BaseList = await LoadoutManager.ManageGame(Install, "BaseList", CancellationToken.None);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
