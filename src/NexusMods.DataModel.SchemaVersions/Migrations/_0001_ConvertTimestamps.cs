using System.Buffers;
using System.Security.Cryptography.X509Certificates;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.DataModel.SchemaVersions.Migrations;

/// <summary>
/// Converts timestamps from Unix file times to Ticks. No data shape changes
/// just the conversion of the data itself. This was a change done to make precision
/// of times more consistent when converted to and from filesystem times.
/// </summary>
public class _0001_ConvertTimestamps : IScanningMigration
{
    private HashSet<AttributeId> _attrIds = new();

    /// <inheritdoc />
    public static (MigrationId Id, string Name) IdAndName => MigrationId.ParseNameAndId(nameof(_0001_ConvertTimestamps));

    /// <inheritdoc />
    public async Task Prepare(IDb db)
    {
        _attrIds = db.Connection.AttributeResolver.DefinedAttributes.Where(a => a is TimestampAttribute)
            .Select(a => db.AttributeCache.GetAttributeId(a.Id))
            .ToHashSet();
    }

    /// <inheritdoc />
    public ScanResultType Update(ref KeyPrefix prefix, ReadOnlySpan<byte> valueSpan, in IBufferWriter<byte> writer)
    {
        if (!_attrIds.Contains(prefix.A))
            return ScanResultType.None;
        
        var oldTimestamp = Int64Serializer.Read(valueSpan);
        var newTimestamp = ConvertTimestamps(oldTimestamp);
        Int64Serializer.Write(newTimestamp, writer);
        return ScanResultType.Update;
    }
    
    /// <summary>
    /// Perform the actual data conversion
    /// </summary>
    private static long ConvertTimestamps(long oldTimestamp)
    {
        try
        {
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(oldTimestamp);
            return dt.UtcTicks;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            // This is a timestamp that is out of range for the DateTimeOffset class, so assume it is already in Ticks.
            return oldTimestamp;
        }
    }
}
