using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI;
using NexusMods.CLI.Verbs;
using NexusMods.DataModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

namespace NexusMods.UI.Tests;

public class AVmTest<TVm> : AUiTest, IAsyncLifetime
where TVm : IViewModelInterface
{
    protected AbsolutePath DataZipLzma => FileSystem.GetKnownPath(KnownPath.EntryDirectory).CombineUnchecked(@"Resources\data_zip_lzma.zip");
    protected AbsolutePath Data7ZLzma2 => FileSystem.GetKnownPath(KnownPath.EntryDirectory).CombineUnchecked(@"Resources\data_7zip_lzma2.7z");

    protected AbsolutePath DataTest =>
        FileSystem.GetKnownPath(KnownPath.EntryDirectory).CombineUnchecked(@"Resources\data.test");
    
    private VMWrapper<TVm> _vmWrapper { get; }
    protected StubbedGame Game { get; }
    protected IFileSystem FileSystem { get; }
    protected GameInstallation Install { get; }
    protected LoadoutManager LoadoutManager { get; }
    protected LoadoutRegistry LoadoutRegistry { get; }
    
    protected IDataStore DataStore { get; }
    
    protected IArchiveAnalyzer ArchiveAnalyzer { get; }
    protected IArchiveInstaller ArchiveInstaller { get; }


    private LoadoutId? _loadoutId;
    protected LoadoutMarker Loadout => _loadoutId != null ? 
        new LoadoutMarker(LoadoutRegistry, _loadoutId.Value) :
        throw new InvalidOperationException("LoadoutId is null");

    public AVmTest(IServiceProvider provider) : base(provider)
    {
        _vmWrapper = GetActivatedViewModel<TVm>();
        DataStore = provider.GetRequiredService<IDataStore>();
        LoadoutManager = provider.GetRequiredService<LoadoutManager>();
        LoadoutRegistry = provider.GetRequiredService<LoadoutRegistry>();
        Game = provider.GetRequiredService<StubbedGame>();
        Install = Game.Installations.First();
        FileSystem = provider.GetRequiredService<IFileSystem>();
        ArchiveAnalyzer = provider.GetRequiredService<IArchiveAnalyzer>();
        ArchiveInstaller = provider.GetRequiredService<IArchiveInstaller>();
    }


    protected TVm Vm => _vmWrapper.VM;

    public async Task InitializeAsync()
    {
        _loadoutId = (await LoadoutManager.ManageGameAsync(Install, "Test")).Value.LoadoutId;
    }

    protected async Task<ModId[]> InstallMod(AbsolutePath path)
    {
        var analyzedFile = await ArchiveAnalyzer.AnalyzeFileAsync(path);
        return await ArchiveInstaller.AddMods(Loadout.Value.LoadoutId, analyzedFile.Hash);
    }

    public Task DisposeAsync()
    {
        _vmWrapper.Dispose();
        return Task.CompletedTask;
    }
}
