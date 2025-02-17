using System.Text;
using DynamicData.Kernel;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.StardewValley.Installers;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;

namespace NexusMods.Games.StardewValley.Tests;

public class StardewValleyInstallersTests(ITestOutputHelper outputHelper, IGameDomainToGameIdMappingCache mappingCache) : ALibraryArchiveInstallerTests<SMAPIModInstaller, StardewValley>(outputHelper)
{
    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        return base.AddServices(services)
            .AddStardewValley()
            .AddUniversalGameLocator<StardewValley>(new Version("1.6.1"));
    }
    
    
    [Theory]
    // Test for Credits.xlsx (archive containing spread-sheet) file being installed correctly
    [InlineData("Portraits_for_Vendors.zip")]
    public async Task CanInstallMod(string filename)
    {
        var fullPath = FileSystem.GetKnownPath(KnownPath.CurrentDirectory).Combine("Resources").Combine(filename);

        var loadout = await CreateLoadout();
        var libraryArchive = await RegisterLocalArchive(fullPath);
        var group = await Install<SMAPIModInstaller>(loadout, libraryArchive);

        var sb = new StringBuilder();
        sb.AppendLine($"{group.AsLoadoutItem().Name}:");
        
        foreach (var firstLevelChild in group.Children.OrderBy(item => item.Name))
        {
            sb.AppendLine($"{firstLevelChild.Name}:");
            firstLevelChild.TryGetAsLoadoutItemGroup(out var childGroup).Should().BeTrue("The child should be a loadout item group.");

            // Print out the children of the group
            childGroup.Children
                .Select(child =>
                {
                    child.TryGetAsLoadoutItemWithTargetPath(out var targetPathItem).Should().BeTrue("The module should contain loadout items with a target path.");
                    targetPathItem.TryGetAsLoadoutFile(out var loadoutFile).Should().BeTrue("The module should contain loadout files.");
                    return (targetPathItem, loadoutFile);
                })
                .OrderBy(tuple => tuple.targetPathItem.TargetPath.Item3)
                .ToList()
                .ForEach(tuple =>
                {
                    var path = tuple.targetPathItem.TargetPath;
                    var hash = tuple.loadoutFile.Hash;
                    var size = tuple.loadoutFile.Size;
                    sb.AppendLine($"  {{{path.Item2}}} {path.Item3} - {size} - {hash} - Stored: {FileStore.HaveFile(hash)}");
                });
            
            childGroup.TryGetAsSMAPIModLoadoutItem(out _).Should().BeTrue("The child should be a smapi mod loadout group.");
        }

        await Verify(sb.ToString()).UseParameters(filename);
    }

    [Fact]
    public async Task Test_NoTopLevel()
    {
        var fullPath = FileSystem.GetKnownPath(KnownPath.CurrentDirectory).Combine("Resources").Combine("Foo.zip");

        var loadout = await CreateLoadout();
        var libraryArchive = await RegisterLocalArchive(fullPath);
        var group = await Install<SMAPIModInstaller>(loadout, libraryArchive);

        group.Children.Count.Should().Be(1);
        group.Children.First().TryGetAsLoadoutItemGroup(out var parent).Should().BeTrue();

        foreach (var child in parent.Children)
        {
            child.TryGetAsLoadoutItemWithTargetPath(out var item).Should().BeTrue();
            var (_, locationId, path) = item.TargetPath;
            locationId.Value.Should().Be(LocationId.Game.Value);
            path.ToString().Should().StartWith("Mods/Foo Mod/");
        }
    }
}
