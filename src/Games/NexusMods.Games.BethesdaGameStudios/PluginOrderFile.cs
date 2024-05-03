using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Extensions.BCL;
using NexusMods.Hashing.xxHash64;

// ReSharper disable AccessToDisposedClosure

namespace NexusMods.Games.BethesdaGameStudios;

public record PluginOrderFile : IFileGenerator
{ 
    static UInt128 IGuidClass.Guid => new(0x3f1b_7b1b_4b1b_8b1b, 0x2b1b_1b1b_9b1b_6b1b);
    
    public static readonly GamePath Path = new(LocationId.AppData, "plugins.txt");
    
    public async ValueTask<Hash?> Write(GeneratedFile.Model generatedFile, Stream stream, 
        Loadout.Model loadout, FlattenedLoadout flattenedLoadout, FileTree fileTree)
    {
        var sorted = await ((ABethesdaGame)loadout.Installation.Game)
            .PluginSorter.Sort(fileTree, CancellationToken.None);

        await stream.WriteAllLinesAsync(sorted.Select(e => "*" + e.FileName));
        return null;
    }
}
