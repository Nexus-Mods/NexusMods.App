using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

namespace NexusMods.UI.Tests;

public class AVmTest<TVm> : AUiTest, IAsyncLifetime
where TVm : IViewModelInterface
{
    private VMWrapper<TVm> _vmWrapper { get; }
    protected StubbedGame Game { get; }
    protected GameInstallation Install { get; }
    protected LoadoutManager LoadoutManager { get; }
    protected LoadoutRegistry LoadoutRegistry { get; }
    
    protected IDataStore DataStore { get; }


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
    }



    public TVm Vm => _vmWrapper.VM;

    public async Task InitializeAsync()
    {
        _loadoutId = (await LoadoutManager.ManageGameAsync(Install, "Test")).Value.LoadoutId;
    }

    public async Task DisposeAsync()
    {
        _vmWrapper.Dispose();
    }
}
