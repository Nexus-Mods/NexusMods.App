using Avalonia.Controls;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.DataModel.Loadouts;
using NexusMods.Paths.Utilities;
using NexusMods.UI.Theme.Controls.Spine.Buttons;

namespace NexusMods.UI.Theme.Tests;

public class SpineViewModelTests
{
    private readonly LoadoutManager _loadoutManager;
    private readonly IServiceProvider _provider;

    public SpineViewModelTests(LoadoutManager manager, IServiceProvider provider)
    {
        _loadoutManager = manager;
        _provider = provider;
    }
    
    [Fact]
    public async Task ActivatingOneButtonDisablesOthers()
    {
        
        var loadout = await _loadoutManager.ImportFrom(KnownFolders.EntryFolder.Join(@"Resources\cyberpunk2077.1.61.zip"));
        var vm = _provider.GetRequiredService<SpineViewModel>();
        using var _ = vm.Activator.Activate();
        vm.Games.Select(g => g.Name).Should().Contain("Cyberpunk 2077");


    }
}