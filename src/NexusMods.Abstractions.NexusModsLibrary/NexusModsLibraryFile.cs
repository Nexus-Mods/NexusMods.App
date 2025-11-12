using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Sdk.Library;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// Represented a <see cref="LibraryItem"/> originating from Nexus Mods.
/// </summary>
[PublicAPI]
[Include<LibraryItem>]
public partial class NexusModsLibraryItem : IModelDefinition
{
    private const string Namespace = "NexusMods.NexusModsLibrary.NexusModsLibraryItem";

    /// <summary>
    /// Remote metadata of the file on Nexus Mods.
    /// </summary>
    public static readonly ReferenceAttribute<NexusModsFileMetadata> FileMetadata = new(Namespace, nameof(FileMetadata));

    /// <summary>
    /// Remote metadata of the mod page on Nexus Mods.
    /// </summary>
    public static readonly ReferenceAttribute<NexusModsModPageMetadata> ModPageMetadata = new(Namespace, nameof(ModPageMetadata));
}
