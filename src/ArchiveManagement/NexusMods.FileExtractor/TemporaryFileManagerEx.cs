using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.Settings;
using NexusMods.Paths;

namespace NexusMods.FileExtractor;

/// <summary>
/// Variation of <see cref="TemporaryFileManager"/> with support for injecting settings via DI.
/// </summary>
internal class TemporaryFileManagerEx : TemporaryFileManager
{
    public TemporaryFileManagerEx(IFileSystem fileSystem, ISettingsManager settingsManager)
        : base(
            fileSystem,
            basePath: settingsManager.Get<FileExtractorSettings>().TempFolderLocation.ToPath(fileSystem)
        ) { }
}
