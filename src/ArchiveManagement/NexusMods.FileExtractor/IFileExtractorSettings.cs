using JetBrains.Annotations;
using NexusMods.Paths;

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
    /// <inheritdoc />
    public ConfigurationPath TempFolderLocation { get; set; }

    /// <summary>
    /// Default constructor for serialization.
    /// </summary>
    public FileExtractorSettings() : this(FileSystem.Shared) { }

    /// <summary>
    /// Creates a default new instance of <see cref="FileExtractorSettings"/>.
    /// </summary>
    /// <param name="fileSystem"></param>
    public FileExtractorSettings(IFileSystem fileSystem)
    {
        TempFolderLocation = new ConfigurationPath(GetDefaultBaseDirectory(fileSystem));
    }

    private static AbsolutePath GetDefaultBaseDirectory(IFileSystem fs)
    {
        return fs.OS.MatchPlatform(
            () => fs.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("NexusMods.App/Temp"),
            () => fs.GetKnownPath(KnownPath.XDG_DATA_HOME).Combine("NexusMods.App/Temp"),
            () => throw new NotSupportedException(
                "(Note: Sewer) Paths needs PR for macOS. I don't have a non-painful way to access a Mac."));
    }

    /// <summary>
    /// Ensures default settings in case of placeholders of undefined/invalid settings.
    /// </summary>
    public void Sanitize(IFileSystem fs)
    {
        // Set default locations if none are provided.
        if (string.IsNullOrEmpty(TempFolderLocation.RawPath))
            TempFolderLocation = new ConfigurationPath(GetDefaultBaseDirectory(fs));

        TempFolderLocation.ToAbsolutePath().CreateDirectory();
    }
}
