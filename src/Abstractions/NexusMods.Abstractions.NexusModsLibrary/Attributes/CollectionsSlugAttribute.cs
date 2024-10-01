using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.NexusModsLibrary.Attributes;

/// <summary>
/// An attribute that holds a <see cref="CollectionSlug"/> value.
/// </summary>
public class CollectionsSlugAttribute(string ns, string name) : ScalarAttribute<CollectionSlug, string>(ValueTags.Ascii, ns, name)
{
    /// <inheritdoc />
    protected override string ToLowLevel(CollectionSlug value)
    {
        return value.Value;
    }

    /// <inheritdoc />
    protected override CollectionSlug FromLowLevel(string value, ValueTags tag, AttributeResolver resolver)
    {
        return CollectionSlug.From(value);
    }
}
