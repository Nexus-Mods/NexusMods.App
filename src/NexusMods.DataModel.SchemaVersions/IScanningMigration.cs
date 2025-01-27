using System.Buffers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.DataModel.SchemaVersions;

public interface IScanningMigration : IMigration
{
    void Prepare(IDb db);

    ScanResultType Update(ref KeyPrefix prefix, ReadOnlySpan<byte> valueSpan, in IBufferWriter<byte> writer);
}
