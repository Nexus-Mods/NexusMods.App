using JetBrains.Annotations;
using NexusMods.Abstractions.NexusModsLibrary.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Sdk.Library;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// A library file that is a Nexus Mods collection download.
/// </summary>
[PublicAPI]
[Include<LibraryFile>]
public partial class NexusModsCollectionLibraryFile : IModelDefinition
{
    private const string Namespace = "NexusMods.NexusModsLibrary.NexusModsCollectionLibraryFile";

    public static readonly CollectionsSlugAttribute CollectionSlug = new(Namespace, nameof(CollectionSlug)) { IsIndexed = true };

    public static readonly RevisionNumberAttribute CollectionRevisionNumber = new(Namespace, nameof(CollectionRevisionNumber)) { IsIndexed = true };
}
