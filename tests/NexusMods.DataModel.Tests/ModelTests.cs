using System.Collections.Immutable;
using System.IO.Compression;
using System.Reactive.Linq;
using FluentAssertions;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Hashing.xxHash64;
using NexusMods.Interfaces;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;
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
            Id = ModFileId.New(),
            To = new GamePath(GameFolderType.Game, "foo/bar.pez"),
            From = new HashRelativePath(Hash.Zero, RelativePath.Empty),
            Hash = (Hash)0x42L,
            Size = Size.From(44L),
            Store = DataStore
        };
        file.Store.Should().NotBeNull();
        file.DataStoreId.Should().NotBeNull();

        DataStore.Get<FromArchive>(file.DataStoreId)!.To.Should().BeEquivalentTo(file.To);
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
        loadout.Value.Mods.Values.Sum(m => m.Files.Count).Should().Be(DATA_NAMES.Length * 2 + StubbedGame.DATA_NAMES.Length);
        
    }

    [Fact]
    public async Task RenamingAListDoesntChangeOldIds()
    {
        var loadout = await LoadoutManager.ManageGame(Install, Guid.NewGuid().ToString());
        var id1 = await loadout.Install(DATA_7Z_LZMA2, "Mod1", CancellationToken.None);
        var id2 = await loadout.Install(DATA_ZIP_LZMA, "Mod2", CancellationToken.None);

        id1.Should().NotBe(id2);
        id1.Should().BeEquivalentTo(id1);
        id2.Should().BeEquivalentTo(id2);

        loadout.Value.Mods.Values.Count(m => m.Id == id1).Should().Be(1);
        loadout.Value.Mods.Values.Count(m => m.Id == id2).Should().Be(1);
        
        var history = loadout.History().Select(h => h.DataStoreId).ToArray();
        history.Length.Should().Be(4);
        
        LoadoutManager.Alter(loadout.Value.LoadoutId, l => l.PreviousVersion.Value);

        var newHistory = loadout.History().Select(h => h.DataStoreId).ToArray();

        newHistory.Skip(1).Should().BeEquivalentTo(history);
        
    }

    [Fact]
    public async Task CanExportAndImportLoadouts()
    {
        var loadout = await LoadoutManager.ManageGame(Install, Guid.NewGuid().ToString());
        var id1 = await loadout.Install(DATA_7Z_LZMA2, "Mod1", CancellationToken.None);
        var id2 = await loadout.Install(DATA_ZIP_LZMA, "Mod2", CancellationToken.None);
        
        var tempFile = TemporaryFileManager.CreateFile(Ext.Zip);
        await loadout.ExportTo(tempFile, CancellationToken.None);

        {
            await using var of = tempFile.Path.Read();
            using var zip = new ZipArchive(of, ZipArchiveMode.Read);
            var entries = zip.Entries.Select(e => e.Name.ToRelativePath().FileName)
                .Select(h => Id.FromTaggedSpan(Convert.FromHexString(h.ToString())))
                .ToHashSet();

            var ids = loadout.Value.Walk((set, itm) => set.Add(itm.DataStoreId),
                ImmutableHashSet<Id>.Empty);
            
            foreach (var id in ids)
                entries.Should().Contain(id);
            
            entries.Should().Contain(loadout.Value.DataStoreId, "The loadout is stored");

            foreach (var mod in loadout.Value.Mods.Values)
            {
                entries.Should().Contain(mod.DataStoreId, "The mod is stored");
                foreach (var file in mod.Files.Values)
                    entries.Should().Contain(file.DataStoreId, "The file is stored (file: {0})", file.To);
            }

        }
    }

}