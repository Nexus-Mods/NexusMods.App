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
        var cache = db.AttributeResolver.AttributeCache;

        
        void AppendLine(string s)
        {
            // We want platform independent newlines.
            sb.Append(s);
            sb.Append("\n");
        }
        
        foreach (var id in cache.AllAttributeIds.OrderBy(id => id.Id, StringComparer.Ordinal))
        {
            var aid = cache.GetAttributeId(id);
            AppendLine(id.ToString());
            AppendLine(cache.GetValueTag(aid).ToString());
            AppendLine(cache.IsIndexed(aid).ToString());
            AppendLine(cache.IsCardinalityMany(aid).ToString());
            AppendLine(cache.IsNoHistory(aid).ToString());
            AppendLine("--");
        }
        // Use ascii as the attribute names must be ascii and this makes data comparisons simpler.
        var bytes = Encoding.ASCII.GetBytes(sb.ToString());
        return bytes.xxHash3();
    }
    
}
