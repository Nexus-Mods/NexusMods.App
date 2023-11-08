using NexusMods.DataModel;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.LoadoutSynchronizer;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using IGeneratedFile = NexusMods.DataModel.LoadoutSynchronizer.IGeneratedFile;

// ReSharper disable AccessToDisposedClosure

namespace NexusMods.Games.BethesdaGameStudios;

[JsonName("BethesdaGameStudios.PluginOrderFile")]
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
        return ValueTask.FromResult<AModFile>(new PluginOrderFile {Id = ModFileId.New()});
    }
}
