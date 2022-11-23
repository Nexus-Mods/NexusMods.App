using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.DataModel.Abstractions;

[JsonDerivedType(typeof(ModFile), "ModFile")]
public interface IVersionedObject
{
    /// <summary>
    /// Returns true if this object has unsaved changes
    /// </summary>
    public bool IsDirty { get; }
    
    /// <summary>
    /// Returns the current of this object, will be a unset ID if the
    /// object has unsaved changes.
    /// </summary>
    public Id Id { get; }
    
    /// <summary>
    /// Add a parent to this object that will receive notifications when
    /// the child goes from clean to dirty
    /// </summary>
    /// <param name="parent"></param>
    public void AddParent(IVersionedParent parent);

    /// <summary>
    /// Stop tracking a parent of this object
    /// </summary>
    /// <param name="parent"></param>
    public void RemoveParent(IVersionedParent parent);
}