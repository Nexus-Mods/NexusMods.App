using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveMetaData;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.GlobalSettings;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

namespace NexusMods.UI.Tests;

public class AVmTest<TVm> : AUiTest, IAsyncLifetime
where TVm : IViewModelInterface
{
    protected AbsolutePath DataZipLzma => FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("Resources/data_zip_lzma.zip");
    protected AbsolutePath Data7ZLzma2 => FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("Resources/data_7zip_lzma2.7z");

    protected AbsolutePath DataTest =>
        FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("Resources/data.test");

    private VMWrapper<TVm> _vmWrapper { get; }
    protected StubbedGame Game { get; }
    protected IFileSystem FileSystem { get; }
    protected GameInstallation Install { get; }
    protected LoadoutRegistry LoadoutRegistry { get; }

    protected IDataStore DataStore { get; }
    protected IArchiveInstaller ArchiveInstaller { get; }

    protected IFileOriginRegistry FileOriginRegistry { get; }
    protected GlobalSettingsManager GlobalSettingsManager { get; }


    private LoadoutMarker? _loadout;
    protected LoadoutMarker Loadout => _loadout!;

    public AVmTest(IServiceProvider provider) : base(provider)
    {
        _vmWrapper = GetActivatedViewModel<TVm>();
        DataStore = provider.GetRequiredService<IDataStore>();
        LoadoutRegistry = provider.GetRequiredService<LoadoutRegistry>();
        Game = provider.GetRequiredService<StubbedGame>();
        Install = Game.Installations.First();
        FileSystem = provider.GetRequiredService<IFileSystem>();
        ArchiveInstaller = provider.GetRequiredService<IArchiveInstaller>();
        FileOriginRegistry = provider.GetRequiredService<IFileOriginRegistry>();
        GlobalSettingsManager = provider.GetRequiredService<GlobalSettingsManager>();
    }


    protected TVm Vm => _vmWrapper.VM;

    public async Task InitializeAsync()
    {
        _loadout = await LoadoutRegistry.Manage(Install, "Test");
    }

    protected async Task<ModId[]> InstallMod(AbsolutePath path)
    {
        var downloadId = await FileOriginRegistry.RegisterDownload(path,
            new FilePathMetadata() { OriginalName = path.FileName, Quality = Quality.Normal });
        return await ArchiveInstaller.AddMods(Loadout.Value.LoadoutId, downloadId);
    }

    public Task DisposeAsync()
    {
        _vmWrapper.Dispose();
        return Task.CompletedTask;
    }
}

public class AVmTest<TVm, TVmInterface> : AVmTest<TVmInterface> where TVmInterface : IViewModelInterface
where TVm : TVmInterface
{
    public AVmTest(IServiceProvider provider) : base(provider) { }

    /// <summary>
    /// The concrete view model, not the interface.
    /// </summary>
    public TVm ConcreteVm => (TVm) Vm;
}
