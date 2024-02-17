using JetBrains.Annotations;
using NexusMods.Paths;

namespace NexusMods.Abstractions.FileExtractor;

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
        // Note: The idiomatic place for this is Temporary Directory (/tmp on Linux, %TEMP% on Windows)
        //       however this can be dangerous to do on Linux, as /tmp is often a RAM disk, and can be
        //       too small to handle large files.
        return fs.OS.MatchPlatform(
            () => fs.GetKnownPath(KnownPath.TempDirectory).Combine("NexusMods.App/Temp"),
            () => fs.GetKnownPath(KnownPath.XDG_STATE_HOME).Combine("NexusMods.App/Temp"),
            // Use _App vs .App so as not to confuse OSX into thinking this is an app bundle
            () => fs.GetKnownPath(KnownPath.TempDirectory).Combine("NexusMods_App/Temp"));
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
