using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

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
        
        var lastTimestamp = DateTimeOffset.UtcNow;
        var lastId = LoadoutId.From(0);
        using var loadouts = Loadout.RevisionsWithChildUpdates(Connection, loadoutId)
            .Subscribe(loadout =>
                {
                    lastId = loadout.Id;
                    lastTimestamp = DateTimeOffset.UtcNow;
                }
            );
        
        fileId = result[fileId];
        groupId = result[groupId];

        await Connection.FlushQueries(); 
        lastId.Should().Be(loadoutId);
        var originalTimestamp = lastTimestamp;
        
        
        // Delete a file and the row should update
        using var tx2 = Connection.BeginTransaction();
        tx2.Delete(fileId, false);
        var result2 = await tx2.Commit();

        await Connection.FlushQueries();
        lastId.Should().Be(loadoutId);
        lastTimestamp.Should().BeAfter(originalTimestamp);
    }
}
