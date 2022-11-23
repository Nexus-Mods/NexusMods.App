using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel;

public abstract class AVersionedObject : IVersionedObject
{
    private Id _id = Id.Empty;
    
    private HashSet<IVersionedParent> _parents = new();

    public bool IsDirty => _id.IsUnset;

    public Id Id => _id;

    public void AddParent(IVersionedParent parent)
    {
        _parents.Add(parent);
    }

    public void RemoveParent(IVersionedParent parent)
    {
        _parents.Remove(parent);
    }
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (_id.IsUnset || propertyName == "Id") return;
        
        _id.Reset();
        NotifyParents();
    }

    protected void NotifyParents()
    {
        foreach (var parent in _parents)
        {
            parent.NotifyChildChanged(this);
        }
    }
}