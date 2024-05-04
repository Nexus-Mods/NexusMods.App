using Bannerlord.LauncherManager.Models;
using FluentAssertions;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.MountAndBlade2Bannerlord.Extensions;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
using NexusMods.Games.MountAndBlade2Bannerlord.Tests.Shared;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests.Utils;

public class LoadoutExtensionsTests : AGameTest<MountAndBlade2Bannerlord>
{
    public LoadoutExtensionsTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    private static LoadoutModuleViewModel ViewModelCreator(Mod.Model mod, ModuleInfoExtendedWithPath moduleInfo, int index) => new()
    {
        Mod = mod,
        ModuleInfoExtended = moduleInfo,
        IsValid = true,
        IsSelected = mod.Enabled,
        IsDisabled = mod.Status == ModStatus.Failed,
        Index = index,
    };

    [Fact]
    public async Task Test_GetViewModels()
    {
        var loadout = await CreateLoadout();

        var context = AGameTestContext.Create(CreateTestArchive, InstallModStoredFileIntoLoadout);

        await loadout.AddButterLib(context);
        await loadout.AddHarmony(context);

        Refresh(ref loadout);

        var unsorted = loadout.GetViewModels(ViewModelCreator).Select(x => x.Mod.Name).ToList();
        var sorted = (await loadout.GetSortedViewModelsAsync(ViewModelCreator)).Select(x => x.Mod.Name).ToList();

        unsorted.Should().BeEquivalentTo([
            "ButterLib",
            "Harmony",
            ]
        );
        sorted.Should().BeEquivalentTo([
            "Harmony",
            "ButterLib",
            ]
        );
    }
}
