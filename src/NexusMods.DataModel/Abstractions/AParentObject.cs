namespace NexusMods.DataModel.Abstractions;

public abstract class AParentObject : AVersionedObject, IVersionedParent
{
    public void NotifyChildChanged(IVersionedObject child)
    {
        if (IsDirty) return;
        Id.Reset();
        NotifyParents();
    }

    public abstract void PersistChildren(IDataStore store);
}