using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda.Plugins;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Sorting;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Games.CreationEngine;
using NexusMods.Games.CreationEngine.LoadOrder;
using NexusMods.Games.TestFramework;
using NexusMods.HyperDuck;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace NexusMods.Games.CreationEngine.Tests.SkyrimSE;

public class PluginLoadOrderIntegrationTests(ITestOutputHelper output)
    : AIsolatedGameTest<PluginLoadOrderIntegrationTests, CreationEngine.SkyrimSE.SkyrimSE>(output)
{
    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        return base.AddServices(services)
            .AddCreationEngine()
            .AddAdapters()
            .AddUniversalGameLocator<CreationEngine.SkyrimSE.SkyrimSE>(new Version("1.6.1"));
    }

    private async Task<(Loadout.ReadOnly loadout, SortOrderId sortOrderId, PluginLoadOrderVariety variety)> SetupLoadoutWithPlugins(params string[] pluginFilenames)
    {
        var loadout = await CreateLoadout();

        using (var tx = Connection.BeginTransaction())
        {
            // Create one group per plugin and add a plugin file under Data/
            foreach (var filename in pluginFilenames)
            {
                var group = AddEmptyGroup(tx, loadout.Id, $"Mod for {filename}");
                AddFile(tx, loadout.Id, group, new GamePath(LocationId.Game, $"Data/{filename}"), content: null);
            }
            await tx.Commit();
        }

        // Build initial sort orders for the loadout
        var manager = InitAndGetSortOrderManager();
        await manager.UpdateLoadOrders(loadout.Id);

        // Get the variety and its sort order id
        var variety = (PluginLoadOrderVariety)manager.GetSortOrderVarieties()
            .First(v => v is PluginLoadOrderVariety);

        var sortOrderId = await variety.GetOrCreateSortOrderFor(
            loadout.Id,
            OneOf.OneOf<LoadoutId, CollectionGroupId>.FromT0(loadout.Id));

        return (loadout, sortOrderId, variety);
    }

    [Fact]
    public async Task PluginsFile_Write_Reflects_Initial_SortOrder()
    {
        var (loadout, _, _) = await SetupLoadoutWithPlugins("A.esm", "B.esp");

        var sorter = ServiceProvider.GetRequiredService<ISorter>();
        var logger = ServiceProvider.GetRequiredService<ILogger<PluginsFile>>();
        var pluginsFile = new PluginsFile(logger, Game, sorter);

        await using var ms = new MemoryStream();
        await pluginsFile.Write(ms, loadout, new Dictionary<GamePath, SyncNode>());
        ms.Position = 0;
        using var reader = new StreamReader(ms, Encoding.UTF8);
        var contents = await reader.ReadToEndAsync();

        // New items are inserted at the start, so B.esp (added second) should come before A.esm
        var lines = contents.Replace("\r", string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(3);
        lines[0].Should().Be("# File maintained by the Nexus Mods app");
        lines[1].Should().Be("*B.esp");
        lines[2].Should().Be("*A.esm");
    }

    [Fact]
    public async Task PluginsFile_Write_Reflects_Moves_And_New_Items()
    {
        var (loadout, sortOrderId, variety) = await SetupLoadoutWithPlugins("A.esm", "B.esp");

        // Move B.esp down by one (from index 0 to 1) so order becomes A.esm, B.esp
        await variety.MoveItemDelta(sortOrderId, new SortItemKey<ModKey>(ModKey.FromFileName("B.esp")), delta: 1);

        var sorter = ServiceProvider.GetRequiredService<ISorter>();
        var logger = ServiceProvider.GetRequiredService<ILogger<PluginsFile>>();
        var pluginsFile = new PluginsFile(logger, Game, sorter);

        await using (var ms = new MemoryStream())
        {
            await pluginsFile.Write(ms, loadout, new Dictionary<GamePath, SyncNode>());
            ms.Position = 0;
            using var reader = new StreamReader(ms, Encoding.UTF8);
            var contents = await reader.ReadToEndAsync();
            var lines = contents.Replace("\r", string.Empty)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            lines.Should().HaveCount(3);
            lines[1].Should().Be("*A.esm");
            lines[2].Should().Be("*B.esp");
        }

        // Add a new plugin C.esl; reconciliation should insert it at the front
        using (var tx = Connection.BeginTransaction())
        {
            var group = AddEmptyGroup(tx, loadout.Id, "Mod for C.esl");
            AddFile(tx, loadout.Id, group, new GamePath(LocationId.Game, "Data/C.esl"), content: null);
            await tx.Commit();
        }

        await InitAndGetSortOrderManager().UpdateLoadOrders(loadout.Id);

        await using (var ms2 = new MemoryStream())
        {
            await pluginsFile.Write(ms2, loadout, new Dictionary<GamePath, SyncNode>());
            ms2.Position = 0;
            using var reader2 = new StreamReader(ms2, Encoding.UTF8);
            var contents2 = await reader2.ReadToEndAsync();
            var lines2 = contents2.Replace("\r", string.Empty)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            lines2.Should().HaveCount(4);
            lines2[1].Should().Be("*C.esl");
            // Prior relative order A before B remains
            lines2[2].Should().Be("*A.esm");
            lines2[3].Should().Be("*B.esp");
        }
    }
}
