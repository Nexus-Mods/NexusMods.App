using System.Text;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel.SchemaVersions;

/// <summary>
/// Tools for generating a hash of all the attributes of a schema so that we can detect changes.
/// </summary>
public class SchemaFingerprint
{
    public static Hash GenerateFingerprint(IDb db)
    {
        StringBuilder sb = new();
        var cache = db.AttributeCache;
        
        foreach (var id in cache.AllAttributeIds.OrderBy(id => id.Id, StringComparer.Ordinal))
        {
            var aid = cache.GetAttributeId(id);
            sb.AppendLine(id.ToString());
            sb.AppendLine(cache.GetValueTag(aid).ToString());
            sb.AppendLine(cache.IsIndexed(aid).ToString());
            sb.AppendLine(cache.IsCardinalityMany(aid).ToString());
            sb.AppendLine(cache.IsNoHistory(aid).ToString());
            sb.AppendLine("--");
        }
        return sb.ToString().xxHash3AsUtf8();
    }
    
}
