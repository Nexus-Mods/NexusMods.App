using FluentAssertions;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.Tests;

namespace NexusMods.DataModel.Tests;

public class ModelTests : ADataModelTest<ModelTests>
{
    
    public ModelTests(IServiceProvider provider) : base(provider)
    {
    }
    
    [Fact]
    public void CanCreateModFile()
    {
        var file = new FromArchive
        {
            To = new GamePath(GameFolderType.Game, "foo/bar.pez"),
            From = new HashRelativePath(new Hash(0), RelativePath.Empty),
            Hash = (Hash)0x42L,
            Size = 44L,
            Store = DataStore
        };
        file.Store.Should().NotBeNull();
        file.Id.Should().NotBeNull();

        DataStore.Get<FromArchive>(file.Id)!.To.Should().BeEquivalentTo(file.To);
    }

    [Fact]
    public async Task CanSeeChangesViaObservable()
    {
        var list = new HashSet<string>();
        
        var loadout = await LoadoutManager.ManageGame(Install, "OldName");
        loadout.Changes.Subscribe(f => list.Add(f.Name));
        loadout.Alter(m => m with {Name = "NewName"});

        loadout.Value.Name.Should().Be("NewName");
        list.Count.Should().Be(1);
        list.First().Should().Be("NewName");
    }
    
    [Fact]
    public async Task CanInstallAMod()
    {
        var name = Guid.NewGuid().ToString();
        var loadout = await LoadoutManager.ManageGame(Install, name);
        await loadout.Install(DATA_7Z_LZMA2, "Mod1", CancellationToken.None);
        await loadout.Install(DATA_ZIP_LZMA, "", CancellationToken.None);

        loadout.Value.Mods.Count.Should().Be(3);
        loadout.Value.Mods.Sum(m => m.Files.Count).Should().Be(DATA_NAMES.Length * 2 + StubbedGame.DATA_NAMES.Length);
        
    }

}