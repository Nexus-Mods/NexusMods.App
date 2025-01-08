using System.Text;
using DynamicData.Kernel;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.StardewValley.Installers;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;

namespace NexusMods.Games.StardewValley.Tests;

public class StardewValleyInstallersTests(ITestOutputHelper outputHelper) : ALibraryArchiveInstallerTests<SMAPIModInstaller, StardewValley>(outputHelper)
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
        var group = await Install(typeof(SMAPIModInstaller), loadout, libraryArchive);
        

        var sb = new StringBuilder();
        sb.AppendLine($"{group.AsLoadoutItem().Name}:");
        
        foreach (var firstLevelChild in group.Children)
        {
            sb.AppendLine($"{firstLevelChild.Name}:");
            firstLevelChild.TryGetAsLoadoutItemGroup(out var childGroup).Should().BeTrue("The child should be a loadout item group.");
            
            foreach (var child in childGroup.Children.OrderBy(x=> x.Name))
            {
                child.TryGetAsLoadoutItemWithTargetPath(out var targetPathItem).Should().BeTrue("The module should contain loadout items with a target path.");
                targetPathItem.TryGetAsLoadoutFile(out var loadoutFile).Should().BeTrue("The module should contain loadout files.");
                sb.AppendLine($"  {{{targetPathItem.TargetPath.Item2}}} {targetPathItem.TargetPath.Item3} - {loadoutFile.Size} - {loadoutFile.Hash}");
            }
            
            childGroup.TryGetAsSMAPIModLoadoutItem(out var redModGroup).Should().BeTrue("The child should be a smapi mod loadout group.");
        }
        
        await Verify(sb.ToString()).UseParameters(filename);
    }
    
    
}
