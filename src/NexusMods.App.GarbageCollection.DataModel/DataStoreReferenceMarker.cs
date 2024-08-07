using NexusMods.Abstractions.Loadouts;
using NexusMods.App.GarbageCollection.Interfaces;
using NexusMods.MnemonicDB.Abstractions;
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
        var datoms = LoadoutFile.All(db);
        var isLoadoutValidDict = new Dictionary<LoadoutId, bool>();
        foreach (var loadoutFile in datoms)
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
