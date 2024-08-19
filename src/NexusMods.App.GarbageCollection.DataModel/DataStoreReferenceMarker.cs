using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.GarbageCollection.Interfaces;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
namespace NexusMods.App.GarbageCollection.DataModel;

/// <summary>
/// This class is responsible for finding all used files within the data store
/// and marking them as 'used'.
/// </summary>
public static class DataStoreReferenceMarker
{
    /// <summary>
    /// This method walks through the data store for all <see cref="LoadoutItem"/>(s)
    /// to determine all files used by the data store.
    /// </summary>
    /// <param name="connection">The connection to the MnemonicDB Database.</param>
    /// <param name="archiveGc">The garbage collector from which to reference count all files.</param>
    public static void MarkUsedFiles<TParsedHeaderState, TFileEntryWrapper>(IConnection connection, ArchiveGarbageCollector<TParsedHeaderState, TFileEntryWrapper> archiveGc)
        where TParsedHeaderState : ICanProvideFileHashes<TFileEntryWrapper>
        where TFileEntryWrapper : IHaveFileHash
    {
        var db = connection.Db;
        var loadoutFiles = LoadoutFile.All(db);
        var isLoadoutValidDict = new Dictionary<LoadoutId, bool>();
        
        // Loadouts will have items like 'Game Files', these do not have a corresponding
        // library item.
        MarkItemsUsedInLoadouts(archiveGc, loadoutFiles, db, isLoadoutValidDict);

        // Loadouts will have items like 'Game Files', these do not have a corresponding
        // library item.
        MarkItemsUsedInLibrary(archiveGc, loadoutFiles, db, isLoadoutValidDict);
    }

    private static void MarkItemsUsedInLibrary<TParsedHeaderState, TFileEntryWrapper>(ArchiveGarbageCollector<TParsedHeaderState, TFileEntryWrapper> archiveGc, Entities<LoadoutFile.ReadOnly> loadoutFiles, IDb db, Dictionary<LoadoutId, bool> isLoadoutValidDict) where TParsedHeaderState : ICanProvideFileHashes<TFileEntryWrapper> where TFileEntryWrapper : IHaveFileHash
    {
        /*
            Note(sewer)
         
            How the whole system is built is not particularly easy to understand
            at first, so here's a short explainer that's also in the docs.

            This is based on reading the implementation of AddLibraryFileJobWorker 
            (which is recursive across a base type, and thus not trivial to follow).

            Essentially, we want to scan for all cases of `LibraryArchiveFileEntry` here.
            These belong to an `LibraryArchive`; which is the primitive you usually
            interact with in the UI to add items. Both are of type `LibraryFile`.
            
            Therefore, we need to scan for all non-retracted `LibraryArchiveFileEntry`
            items.
            
            ---------------
            
            There is however a caveat that has to be considered here. Essentially, 
            `LibraryFile` itself has a `Hash` attribute, so it's possible that in
            the future there may be more derivatives. Therefore, just in case,
            we should scan for all `LibraryFile` items that are not retracted.
            
            A false positive is better than a false negative in the context of a GC.
            False positive may have unoptimal disk usage, but at least it wouldn't
            break the system.
         */
        
        var libraryFiles = LibraryFile.All(db);
        foreach (var file in libraryFiles)
            archiveGc.AddReferencedFile(file.Hash);
    }

    private static void MarkItemsUsedInLoadouts<TParsedHeaderState, TFileEntryWrapper>(ArchiveGarbageCollector<TParsedHeaderState, TFileEntryWrapper> archiveGc, Entities<LoadoutFile.ReadOnly> loadoutFiles, IDb db, Dictionary<LoadoutId, bool> isLoadoutValidDict)
        where TParsedHeaderState : ICanProvideFileHashes<TFileEntryWrapper> where TFileEntryWrapper : IHaveFileHash
    {
        foreach (var loadoutFile in loadoutFiles)
        {
            // TODO: Implement recursive includes, e.g. AsLoadoutItemWithTargetPath + AsLoadoutItem
            // into a single method.
            var loadoutItem = new LoadoutItem.ReadOnly(db, loadoutFile.Id);
            if (!isLoadoutValidDict.TryGetValue(loadoutItem.LoadoutId, out var isLoadoutValid))
            {
                var loadout = loadoutItem.Loadout;
                isLoadoutValid = loadout.IsValid();
                isLoadoutValidDict[loadoutItem.LoadoutId] = isLoadoutValid;
            }
            
            if (!isLoadoutValid)
                continue;
            
            // If the loadout is valid, mark the file as used.
            archiveGc.AddReferencedFile(loadoutFile.Hash);
        }
    }
}
