using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Networking.ModUpdates.Traits;
namespace NexusMods.Networking.ModUpdates.Mixins;

/// <summary>
/// A mixin for mod file metadata that allows you to integrate mod file results
/// from the Nexus API.
/// </summary>
public class ModFileMetadataMixin : IAmAModFile
{
    /// <inheritdoc />
    public string Name => Metadata.Name;

    /// <inheritdoc />
    public string Version => Metadata.Version;

    /// <inheritdoc />
    public DateTimeOffset UploadedAt => Metadata.UploadedAt;

    /// <inheritdoc />
    public IEnumerable<IAmAModFile> OtherFilesInSameModPage => 
        Metadata.ModPage.Files.Select(f => new ModFileMetadataMixin(f));

    /// <summary/>
    public NexusModsFileMetadata.ReadOnly Metadata { get; }

    /// <summary/>
    public ModFileMetadataMixin(NexusModsFileMetadata.ReadOnly metadata) => Metadata = metadata;
    
    /// <summary>
    /// Returns a normalized name and version.
    /// </summary>
    public string GetNormalizedFileName() => FuzzySearch.NormalizeFileName(Name, Version);
}
