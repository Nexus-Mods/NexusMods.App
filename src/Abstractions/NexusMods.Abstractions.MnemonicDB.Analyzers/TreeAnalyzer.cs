using System.Collections.Frozen;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.Abstractions.MnemonicDB.Analyzers;

public class TreeAnalyzer : ITreeAnalyzer, IDisposable
{
    public IObservable<TreeUpdate> Updates => _updates;
    public IConnection Connection { get; }

    private readonly Subject<TreeUpdate> _updates = new();
    private readonly IDisposable _disposable;
    private readonly Stack<EntityId> _remaining = new();

    public TreeAnalyzer(IConnection connection)
    {
        Connection = connection;
        _disposable = connection.Revisions.Synchronize().Subscribe(ProcessUpdate);
    }

    public void ProcessUpdate(IDb db)
    {
        _remaining.Clear();
        
        HashSet<EntityId> modified = new();
        
        foreach (var datom in db.RecentlyAdded)
        {
            _remaining.Push(datom.E);
        }
        
        while (_remaining.Count > 0)
        {
            var current = _remaining.Pop();
            
            if (!modified.Add(current))
                continue;
            
            var entity = db.Get(current);
            foreach (var datom in entity)
            {
                var resolved = db.Registry.GetAttribute(datom.A);
                if (resolved is not ReferenceAttribute reference) 
                    continue;
                
                var parent = reference.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag);
                _remaining.Push(parent);
            }
        }

        _updates.OnNext(new TreeUpdate(db, modified.ToFrozenSet()));
    }

    public void Dispose()
    {
        _updates.Dispose();
        _disposable.Dispose();
    }
}
