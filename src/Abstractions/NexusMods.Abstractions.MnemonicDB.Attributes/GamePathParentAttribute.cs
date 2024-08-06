using NexusMods.Abstractions.GameLocators;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// An attribute that combines an EntityId, LocationId and RelativePath into a single attribute. This is used to represent GamePaths prefixed
/// with a parent entity so that range queries only return the paths that are children of the parent entity.
/// </summary>
public class GamePathParentAttribute(string ns, string name) : TupleAttribute<EntityId, ulong, LocationId, string, RelativePath, string>(ValueTags.Reference, ValueTags.Ascii, ValueTags.Utf8, ns, name) 
{
    /// <inheritdoc />
    protected override (EntityId, LocationId, RelativePath) FromLowLevel((ulong, string, string) value)
    {
        return (EntityId.From(value.Item1), LocationId.From(value.Item2), RelativePath.FromUnsanitizedInput(value.Item3));
    }

    /// <inheritdoc />
    protected override (ulong, string, string) ToLowLevel((EntityId, LocationId, RelativePath) value)
    {
        return (value.Item1.Value, value.Item2.Value, value.Item3);
    }
}
