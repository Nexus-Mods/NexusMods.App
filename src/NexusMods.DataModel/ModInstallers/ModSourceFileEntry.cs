using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.ModInstallers;

public class ModSourceFileEntry
{
    private IArchiveManager _manager;

    /// <summary>
    /// Creates a new instance of <see cref="ModSourceFileEntry"/> given the provided <paramref name="manager"/>.
    /// </summary>
    /// <param name="manager"></param>
    public ModSourceFileEntry(IArchiveManager manager)
    {
        _manager = manager;
    }

    /// <summary>
    /// The hash of the file
    /// </summary>
    public required Hash Hash { get; init; }

    /// <summary>
    /// The size of the file
    /// </summary>
    public required Size Size { get; init; }

    /// <summary>
    /// Open the file as a readonly seekable stream
    /// </summary>
    /// <returns></returns>
    public async ValueTask<Stream> Open()
    {
        return await _manager.GetFileStream(Hash);
    }

    /// <summary>
    /// Maps the provided <see cref="ModSourceFileEntry"/> to a <see cref="FromArchive"/> mod file
    /// </summary>
    /// <param name="to"></param>
    /// <returns></returns>
    public FromArchive ToFromArchive(GamePath to)
    {
        return new FromArchive
        {
            Id = ModFileId.New(),
            To = to,
            Hash = Hash,
            Size = Size
        };
    }

}
