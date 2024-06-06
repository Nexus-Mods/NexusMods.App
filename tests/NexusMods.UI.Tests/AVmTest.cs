using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.App.UI;
using NexusMods.MnemonicDB.Abstractions;
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
    protected GameInstallation Install { get; set; }
    protected IConnection Connection { get; }
    protected IArchiveInstaller ArchiveInstaller { get; }
    
    protected IGameRegistry GameRegistry { get; }


    protected IFileOriginRegistry FileOriginRegistry { get; }

    private Loadout.Model? _loadout;
    protected Loadout.Model Loadout
    {
        get
        {
            if (_loadout == null)
                throw new Exception("Must call CreateLoadout before accessing Loadout.");
            return _loadout!;
        }
    }

    public AVmTest(IServiceProvider provider) : base(provider)
    {
        _vmWrapper = GetActivatedViewModel<TVm>();
        Connection = provider.GetRequiredService<IConnection>();
        Game = provider.GetRequiredService<StubbedGame>();
        GameRegistry = provider.GetRequiredService<IGameRegistry>();
        FileSystem = provider.GetRequiredService<IFileSystem>();
        ArchiveInstaller = provider.GetRequiredService<IArchiveInstaller>();
        FileOriginRegistry = provider.GetRequiredService<IFileOriginRegistry>();
    }


    protected TVm Vm => _vmWrapper.VM;

    public async Task CreateLoadout()
    {
        _loadout = await ((IGame)Install.Game).Synchronizer.CreateLoadout(Install, "Test");
    }

    protected async Task<ModId[]> InstallMod(AbsolutePath path)
    {
        var downloadId = await FileOriginRegistry.RegisterDownload(path,
            (tx, id) =>
            {
                tx.Add(id, FilePathMetadata.OriginalName, path.FileName);
            }, path.FileName);
        return await ArchiveInstaller.AddMods(Loadout.LoadoutId, downloadId, path.FileName);
    }

    public async Task InitializeAsync()
    {
        var game = await StubbedGame.Create(Provider);
        Install = game;
    }

    public Task DisposeAsync()
    {
        _vmWrapper.Dispose();
        return Task.CompletedTask;
    }
}
