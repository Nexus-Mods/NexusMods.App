using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveMetaData;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

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
    protected readonly IArchiveManager ArchiveManager;
    protected readonly IArchiveInstaller ArchiveInstaller;
    protected readonly LoadoutManager LoadoutManager;
    protected readonly LoadoutRegistry LoadoutRegistry;
    protected readonly Loadouts.LoadoutSynchronizer LoadoutSynchronizer;
    protected readonly FileHashCache FileHashCache;
    protected readonly IFileSystem FileSystem;
    protected readonly IDataStore DataStore;
    protected readonly IDownloadRegistry DownloadRegistry;
    protected readonly DiskStateRegistry DiskStateRegistry;
    protected readonly IToolManager ToolManager;

    protected readonly IGame Game;
    protected readonly GameInstallation Install;
    protected LoadoutMarker BaseList; // set via InitializeAsync
    protected readonly ILogger<T> Logger;
    private readonly IHost _host;

    protected CancellationToken Token = CancellationToken.None;

    protected ADataModelTest(IServiceProvider provider)
    {
        var provider1 = provider;
        ArchiveManager = provider1.GetRequiredService<IArchiveManager>();
        ArchiveInstaller = provider1.GetRequiredService<IArchiveInstaller>();
        LoadoutManager = provider1.GetRequiredService<LoadoutManager>();
        LoadoutRegistry = provider1.GetRequiredService<LoadoutRegistry>();
        FileHashCache = provider1.GetRequiredService<FileHashCache>();
        FileSystem = provider1.GetRequiredService<IFileSystem>();
        DataStore = provider1.GetRequiredService<IDataStore>();
        DownloadRegistry = provider1.GetRequiredService<IDownloadRegistry>();
        DiskStateRegistry = provider1.GetRequiredService<DiskStateRegistry>();
        Logger = provider1.GetRequiredService<ILogger<T>>();
        LoadoutSynchronizer = provider1.GetRequiredService<Loadouts.LoadoutSynchronizer>();
        TemporaryFileManager = provider1.GetRequiredService<TemporaryFileManager>();
        ToolManager = provider1.GetRequiredService<IToolManager>();
        ServiceProvider = provider;

        Game = provider1.GetRequiredService<StubbedGame>();
        Install = Game.Installations.First();
    }

    public void Dispose()
    {
    }

    public virtual async Task InitializeAsync()
    {
        ((StubbedGame)Game).ResetGameFolders();
        BaseList = LoadoutRegistry.GetMarker((await Install.Game.Synchronizer.Manage(Install)).LoadoutId);
    }

    protected async Task<ModId[]> AddMods(LoadoutMarker mainList, AbsolutePath path, string? name = null)
    {
        var downloadId = await DownloadRegistry.RegisterDownload(path,
            new FilePathMetadata {OriginalName = path.FileName, Quality = Quality.Low}, CancellationToken.None);
        return await ArchiveInstaller.AddMods(mainList.Value.LoadoutId, downloadId, name, CancellationToken.None);
    }

    /// <summary>
    /// Creates a download from the given files, and data (saved as UTF-8 strings), and registers it with the download registry,
    /// returning the download id.
    /// </summary>
    /// <param name="files"></param>
    /// <returns></returns>
    protected async Task<DownloadId> RegisterDownload(params (string Name, string Data)[] files)
    {
        await using var tmpFile = TemporaryFileManager.CreateFile();
        using (var zip = new ZipArchive(tmpFile.Path.Create(), ZipArchiveMode.Create, false))
        {
            foreach (var (name, data) in files)
            {
                var entry = zip.CreateEntry(name);
                await using var stream = entry.Open();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(data);
            }
        }

        return await DownloadRegistry.RegisterDownload(tmpFile.Path, new FilePathMetadata {OriginalName = tmpFile.Path.FileName, Quality = Quality.Low}, CancellationToken.None);
    }

    /// <summary>
    /// Adds a mod to the given loadout, with the given files (saved as UTF-8 strings), and returns the mod id.
    /// </summary>
    /// <param name="modName"></param>
    /// <param name="files"></param>
    /// <returns></returns>
    protected async Task<ModId> AddMod(string modName, params (string Name, string Data)[] files)
    {
        var downloadId = await RegisterDownload(files);
        var modIds = await ArchiveInstaller.AddMods(BaseList.Value.LoadoutId, downloadId, modName, CancellationToken.None);
        return modIds.First();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
