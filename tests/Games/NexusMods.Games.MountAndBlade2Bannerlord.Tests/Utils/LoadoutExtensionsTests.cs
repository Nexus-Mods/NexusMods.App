using Bannerlord.LauncherManager.Models;
using FluentAssertions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.Games.MountAndBlade2Bannerlord.Extensions;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
using NexusMods.Games.MountAndBlade2Bannerlord.Tests.Shared;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests.Utils;

public class LoadoutExtensionsTests : AGameTest<MountAndBlade2Bannerlord>
{
    public LoadoutExtensionsTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    private static LoadoutModuleViewModel ViewModelCreator(Mod mod, ModuleInfoExtendedWithPath moduleInfo, int index) => new()
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
        var loadoutMarker = await CreateLoadout();

        var context = AGameTestContext.Create(CreateTestArchive, InstallModStoredFileIntoLoadout);

        await loadoutMarker.AddButterLib(context);
        await loadoutMarker.AddHarmony(context);

        var unsorted = loadoutMarker.Value.GetViewModels(ViewModelCreator).Select(x => x.Mod.Name).ToList();
        var sorted = (await loadoutMarker.Value.GetSortedViewModelsAsync(ViewModelCreator)).Select(x => x.Mod.Name).ToList();

        unsorted.Should().BeEquivalentTo(new[]
        {
            "ButterLib",
            "Harmony",
        });
        sorted.Should().BeEquivalentTo(new[]
        {
            "Harmony",
            "ButterLib",
        });
    }
}
