using NexusMods.App.GarbageCollection.Structs;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions.Nx.FileProviders;
using NexusMods.Paths.Utilities;
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
    public static void RepackArchive(IProgress<double> progress, List<Hash> toArchive, List<Hash> toRemove, ArchiveReference<NxParsedHeaderState> archive)
    {
        RepackArchive(progress, toArchive, toRemove, archive, true, out _);
    }
    
    /// <summary>
    ///     The method to pass to the 'CollectGarbage' method that
    ///     can be used to repack Nx archives. See <see cref="ArchiveGarbageCollector{TParsedHeaderState,TFileEntryWrapper}.RepackDelegate"/>
    ///     type for more info.
    /// </summary>
    // ReSharper disable once UnusedParameter.Global
    public static void RepackArchive(IProgress<double> progress, List<Hash> toArchive, List<Hash> toRemove, ArchiveReference<NxParsedHeaderState> archive, bool deleteOriginal, out AbsolutePath newArchivePath)
    {
        // If there's nothing to pack, skip the packing.
        var fs = archive.FilePath.FileSystem;
        newArchivePath = default(AbsolutePath);
        if (toArchive.Count <= 0)
            goto end;
        
        // Get the entries that need repacking.
        var nxHeaderItemsByHash = archive.HeaderState.Header.Entries.ToDictionary(entry => (Hash)entry.Hash);
        var entries = new List<FileEntry>(toArchive.Count);
        
        foreach (var hash in toArchive)
        {
            if (nxHeaderItemsByHash.TryGetValue(hash, out var entry))
                entries.Add(entry);
        }
        
        var repacker = new NxRepackerBuilder();
        var fromAbsolutePathProvider = new FromAbsolutePathProvider
        {
            FilePath = archive.FilePath,
        };
        
        var guid = Guid.NewGuid();
        var id = guid.ToString();
        var tmpArchivePath = archive.FilePath.Parent.Combine(id).AppendExtension(KnownExtensions.Tmp);
        repacker.AddFilesFromNxArchive(fromAbsolutePathProvider, archive.HeaderState.Header, entries)
            .WithProgress(progress)
            .WithOutput(tmpArchivePath.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None));
        repacker.Build();
        // Note: There's a flaw with Nx API, the `Build` method should be virtual,
        //       will fix soonish.
        
        // Delete the original archive.
        newArchivePath = tmpArchivePath.ReplaceExtension(KnownExtensions.Nx);
        tmpArchivePath.FileSystem.MoveFile(tmpArchivePath, newArchivePath, true);
        
        end:
        if (deleteOriginal)
            fs.DeleteFile(archive.FilePath);
    }
}
