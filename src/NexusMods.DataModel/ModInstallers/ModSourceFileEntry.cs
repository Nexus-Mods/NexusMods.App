using NexusMods.DataModel.Abstractions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.ModInstallers;

public class ModSourceFileEntry
{
    private IArchiveManager _manager;

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

}
