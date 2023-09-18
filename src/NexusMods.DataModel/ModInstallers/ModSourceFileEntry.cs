using NexusMods.Common;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.ModInstallers;

/// <summary>
/// A helper class for providing information about a mod file to an installer
/// </summary>
public class ModSourceFileEntry
{
    /// <summary>
    /// A factory that can be used to open the file and read its contents
    /// </summary>
    public required IStreamFactory StreamFactory { get; init; }

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
        return await StreamFactory.GetStreamAsync();
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
