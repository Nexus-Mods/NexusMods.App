using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.NexusModsLibrary.Attributes;

/// <summary>
/// An attribute that holds a <see cref="CollectionSlug"/> value.
/// </summary>
public class CollectionsSlugAttribute(string ns, string name) : ScalarAttribute<CollectionSlug, string, AsciiSerializer>(ns, name)
{
    /// <inheritdoc />
    public override string ToLowLevel(CollectionSlug value) => value.Value;

    /// <inheritdoc />
    public override CollectionSlug FromLowLevel(string value, AttributeResolver resolver) => CollectionSlug.From(value);
}
