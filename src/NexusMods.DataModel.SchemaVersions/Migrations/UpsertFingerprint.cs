using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.DataModel.Migrations.Migrations;

public class UpsertFingerprint : IMigration
{
    public string Name => "Upsert Fingerprint";
    public string Description => "Upserts the fingerprint of the database, creating it if it does not exist.";

    /// <summary>
    /// Max value so it always runs last
    /// </summary>
    public DateTimeOffset CreatedAt => DateTimeOffset.MaxValue;
    
    public bool ShouldRun(IDb db)
    {
        if (!db.AttributeCache.Has(SchemaVersion.Fingerprint.Id))
            return true;
        
        var fingerprints = db.Datoms(SchemaVersion.Fingerprint);
        // No fingerprint, we need to create it
        if (fingerprints.Count == 0)
            return true;
        
        var currentFingerprint = SchemaFingerprint.GenerateFingerprint(db);
        var dbFingerprint = Hash.From(ValueTag.UInt64.Read<ulong>(fingerprints.First().ValueSpan));
        // Is the fingerprint up to date?
        return currentFingerprint != dbFingerprint;
    }

    public void Migrate(IDb basis, ITransaction tx)
    {
        var eid = basis.Datoms(SchemaVersion.Fingerprint).Select(d => d.E)
            .FirstOrDefault(tx.TempId());
        
        tx.Add(eid, SchemaVersion.Fingerprint, SchemaFingerprint.GenerateFingerprint(basis));
    }
}
