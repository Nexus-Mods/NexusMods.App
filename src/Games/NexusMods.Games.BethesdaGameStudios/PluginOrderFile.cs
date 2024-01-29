using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Extensions.BCL;
using NexusMods.Hashing.xxHash64;

// ReSharper disable AccessToDisposedClosure

namespace NexusMods.Games.BethesdaGameStudios;

[JsonName("NexusMods.Games.BethesdaGameStudios.PluginOrderFile")]
public record PluginOrderFile : AModFile, IGeneratedFile, IToFile
{
    public static GamePath Path = new(LocationId.AppData, "plugins.txt");

    public GamePath To => Path;

    public async ValueTask<Hash?> Write(Stream stream, Loadout loadout, FlattenedLoadout flattenedLoadout, FileTree fileTree)
    {
        var sorted = await ((ABethesdaGame)loadout.Installation.Game)
            .PluginSorter.Sort(fileTree, CancellationToken.None);

        await stream.WriteAllLinesAsync(sorted.Select(e => "*" + e.FileName));
        return null;
    }

    public ValueTask<AModFile> Update(DiskStateEntry newEntry, Stream stream)
    {
        return ValueTask.FromResult<AModFile>(new PluginOrderFile {Id = ModFileId.NewId()});
    }
}
