using NexusMods.App.GarbageCollection.Nx;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.Paths.Extensions.Nx.FileProviders;
namespace NexusMods.App.GarbageCollection.DataModel.Tests.FindUsedFiles;

internal static class Helpers
{
    internal static NxParsedHeaderState GetParsedNxHeader(AbsolutePath path)
    {
        var streamProvider = new FromAbsolutePathProvider { FilePath = path };
        return new NxParsedHeaderState(HeaderParser.ParseHeader(streamProvider));
    }

    internal static bool IsFileReferenced(ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper> gc, Hash hash)
    {
        if (!gc.HashToArchive.TryGetValue(hash, out var archiveRef))
            return false;

        if (archiveRef.Entries.TryGetValue(hash, out var entry))
            return entry.GetRefCount() > 0;

        return false;
    }
}
