using System.Collections.Frozen;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.Abstractions.MnemonicDB.Analyzers;

/// <inheritdoc />
public class TreeAnalyzer : IAnalyzer<FrozenSet<EntityId>>
{
    /// <inheritdoc />
    public object Analyze(IDb? dbOld, IDb dbNew)
    {
        var remaining = new Stack<(EntityId E, bool IsRetract)>();
        
        dbOld ??= dbNew.Connection.AsOf(TxId.From(dbNew.BasisTxId.Value - 1));
        
        HashSet<EntityId> modified = new();
        
        foreach (var datom in dbNew.RecentlyAdded)
        {
            remaining.Push((datom.E, datom.IsRetract));
        }
        
        while (remaining.Count > 0)
        {
            var current = remaining.Pop();
            
            if (!modified.Add(current.E))
                continue;
            
            var db = current.IsRetract ? dbOld : dbNew;
            var entity = db.Get(current.E);
            foreach (var datom in entity)
            {
                var resolved = dbNew.Registry.GetAttribute(datom.A);
                if (resolved is not ReferenceAttribute reference) 
                    continue;
                
                var parent = reference.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, entity.RegistryId);
                remaining.Push((parent, current.IsRetract));
            }
        }

        return modified.ToFrozenSet();
    }
}
