using NexusMods.App.GarbageCollection.Structs;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions.Nx.FileProviders;
namespace NexusMods.App.GarbageCollection.Nx;

/// <summary>
///     This contains the code responsible for repacking the actual Nx archives
///     as part of the Garbage Collection process.
/// </summary>
public static class NxRepacker
{
    /// <summary>
    ///     The method to pass to the 'CollectGarbage' method that
    ///     can be used to repack Nx archives. See <see cref="ArchiveGarbageCollector{TParsedHeaderState,TFileEntryWrapper}.RepackDelegate"/>
    ///     type for more info.
    /// </summary>
    public static void RepackArchive(IProgress<double> progress, List<Hash> hashes, ArchiveReference<NxParsedHeaderState> archive)
    {
        // Get the entries that need repacking.
        var nxHeaderItemsByHash = archive.HeaderState.Header.Entries.ToDictionary(entry => (Hash)entry.Hash);
        var entries = new List<FileEntry>(hashes.Count);
        
        foreach (var hash in hashes)
        {
            if (nxHeaderItemsByHash.TryGetValue(hash, out var entry))
                entries.Add(entry);
        }

        var repacker = new NxRepackerBuilder();
        var fromAbsolutePathProvider = new FromAbsolutePathProvider
        {
            FilePath = archive.FilePath,
        };

        var tempFile = archive.FilePath.AppendExtension((Extension)".tmp");
        repacker.AddFilesFromNxArchive(fromAbsolutePathProvider, archive.HeaderState.Header, entries)
            .WithProgress(progress)
            .WithOutput(tempFile.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None));
        repacker.Build();
        // Note: There's a flaw with Nx API, the `Build` method should be virtual,
        //       will fix soonish.
        tempFile.FileSystem.MoveFile(tempFile, archive.FilePath, true);
    }
}
