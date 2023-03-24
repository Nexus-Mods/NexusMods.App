using JetBrains.Annotations;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.FileExtractor;

/// <summary>
/// Settings for the file extractor.
/// </summary>
[PublicAPI]
public interface IFileExtractorSettings
{
    /// <summary>
    /// Location of where the temporary folder will be stored.
    /// </summary>
    public ConfigurationPath TempFolderLocation { get; }
}

/// <summary>
/// Default implementation of <see cref="IFileExtractorSettings"/> for reference.
/// </summary>
[PublicAPI]
public class FileExtractorSettings : IFileExtractorSettings
{
    // Note: We can't serialize AbsolutePath because it contains more fields than expected. Just hope user sets correct paths and pray for the best.

    /// <inheritdoc />
    public ConfigurationPath TempFolderLocation { get; set; } = KnownFolders.EntryFolder.CombineUnchecked("Temp").GetFullPath();
}
