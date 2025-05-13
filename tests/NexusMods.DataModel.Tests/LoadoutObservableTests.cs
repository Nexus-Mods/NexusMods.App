using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Rows;
using NexusMods.Cascade;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.DataModel.Tests;

public class LoadoutObservableTests(IServiceProvider provider) : AGameTest<Cyberpunk2077Game>(provider)
{
    [Fact]
    public async Task DeletingAModShouldUpdateTheLoadout()
    {
        using var loadouts = await Connection.Topology.QueryAsync(Loadout.MostRecentTxForLoadout);
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

        await Connection.Topology.FlushEffectsAsync();
        var firstRow = loadouts.First();
        firstRow.RowId.Should().Be(loadoutId);
        var originalTx = firstRow.TxId.Value;

        
        // Delete a file and the row should update
        using var tx2 = Connection.BeginTransaction();
        tx2.Delete(fileId, false);
        var result2 = await tx2.Commit();
        await Connection.Topology.FlushEffectsAsync();
        firstRow.RowId.Should().Be(loadoutId);
        firstRow.TxId.Should().NotBe(originalTx);
        

    }
}
