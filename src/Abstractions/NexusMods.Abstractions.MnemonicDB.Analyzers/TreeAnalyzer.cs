using System.Collections.Frozen;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.Abstractions.MnemonicDB.Analyzers;

public class TreeAnalyzer : IAnalyzer<FrozenSet<EntityId>>
{ 
    public object Analyze(IDb db)
    {
        var remaining = new Stack<EntityId>();
        
        HashSet<EntityId> modified = new();
        
       foreach (var datom in db.RecentlyAdded)
        {
            remaining.Push(datom.E);
        }
        
        while (remaining.Count > 0)
        {
            var current = remaining.Pop();
            
            if (!modified.Add(current))
                continue;
            
            var entity = db.Get(current);
            foreach (var datom in entity)
            {
                var resolved = db.Registry.GetAttribute(datom.A);
                if (resolved is not ReferenceAttribute reference) 
                    continue;
                
                var parent = reference.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, entity.RegistryId);
                remaining.Push(parent);
            }
        }

        return modified.ToFrozenSet();
    }
}
