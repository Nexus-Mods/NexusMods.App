using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ModLists;
using NexusMods.Interfaces;
using NexusMods.Interfaces.Components;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.Tests;
using Xunit.DependencyInjection;

namespace NexusMods.DataModel.Tests.Harness;

public abstract class ADataModelTest<T> : IDisposable
{
    public static readonly AbsolutePath DATA_ZIP_LZMA = KnownFolders.EntryFolder.Combine(@"Resources\data_zip_lzma.zip");
    
    private readonly IServiceProvider _provider;
    protected readonly TemporaryFileManager TemporaryFileManager;
    protected readonly ArchiveContentsCache ArchiveContentsCache;
    protected readonly ModListManager ModListManager;
    protected readonly FileHashCache FileHashCache;
    protected readonly IDataStore DataStore;

    protected readonly IGame Game;
    protected readonly GameInstallation Install;
    protected readonly ILogger<T> _logger;
    private readonly IHost _host;

    protected ADataModelTest(IServiceProvider provider)
    {
        var startup = new Startup();
        _host = Host.CreateDefaultBuilder(Environment.GetCommandLineArgs())
            .ConfigureServices((host, service) => startup.ConfigureServices(service))
            .Build();
        _provider = _host.Services;
        ArchiveContentsCache = _provider.GetRequiredService<ArchiveContentsCache>();
        ModListManager = _provider.GetRequiredService<ModListManager>();
        FileHashCache = _provider.GetRequiredService<FileHashCache>();
        DataStore = _provider.GetRequiredService<IDataStore>();
        _logger = _provider.GetRequiredService<ILogger<T>>();
        TemporaryFileManager = _provider.GetRequiredService<TemporaryFileManager>();
        
        Game = _provider.GetRequiredService<StubbedGame>();
        Install = Game.Installations.First();
        startup.Configure(_provider.GetRequiredService<ILoggerFactory>(), provider.GetRequiredService<ITestOutputHelperAccessor>());

    }


    public void Dispose()
    {
        _host.Dispose();
    }
}