using System.Buffers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.DataModel.SchemaVersions;

/// <summary>
/// A fairly simple but computationally expensive migration. It scans the entire database and updates the data
/// in-place.
/// </summary>
public interface IScanningMigration : IMigration
{
    /// <summary>
    /// Called for every datom in the database. Return .Update to indicate that the data in `prefix` and `writer` have
    /// been populated with new data. Return .Delete to delete the datom. Return .None to do nothing.
    /// </summary>
    ScanResultType Update(ref KeyPrefix prefix, ReadOnlySpan<byte> valueSpan, in IBufferWriter<byte> writer);
}
