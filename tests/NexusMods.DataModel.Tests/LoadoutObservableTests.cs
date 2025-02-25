using System.Collections.Frozen;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Alias;
using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Analyzers;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

namespace NexusMods.DataModel.Tests;

public class LoadoutObservableTests(IServiceProvider provider) : AGameTest<Cyberpunk2077Game>(provider)
{
    [Fact]
    public async Task DeletingAModShouldUpdateTheLoadout()
    {
        using var tx = Connection.BeginTransaction();
        var loadoutId = tx.TempId();

        var loadout = new Loadout.New(tx, loadoutId)
        {
            Name = "Test Loadout",
            ShortName = "B",
            InstallationId = GameInstallation.GameMetadataId,
            LoadoutKind = LoadoutKind.Default,
            Revision = 0,
            GameVersion = VanityVersion.From("Unknown"),
        };

        var group = new LoadoutItemGroup.New(tx, out var groupId)
        {
            IsGroup = true,
            LoadoutItem = new LoadoutItem.New(tx, groupId)
            {
                LoadoutId = loadoutId,
                Name = "Test Group",
            }
        };
        
        var fileId = tx.TempId();
        var file = new LoadoutItem.New(tx, fileId)
        {
            LoadoutId = loadoutId,
            Name = "Test Mod",
            ParentId = groupId,
        };


        var result = await tx.Commit();
        loadoutId = result[loadoutId];
        fileId = result[fileId];
        groupId = result[groupId];

        var changedItems = new List<EntityId>();

        using var updates = Connection.Revisions.Select(r => r.AnalyzerData<TreeAnalyzer, FrozenSet<EntityId>>())
                .Subscribe(itm => changedItems.Add(itm));
        
        changedItems.Clear();
        
        using var tx2 = Connection.BeginTransaction();
        tx2.Delete(fileId, false);
        await tx2.Commit();
        
        changedItems.Should().Contain(loadoutId);
        changedItems.Should().Contain(groupId);
        changedItems.Should().Contain(fileId);

    }
}
