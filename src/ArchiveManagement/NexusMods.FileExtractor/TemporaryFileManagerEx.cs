using NexusMods.Paths;

namespace NexusMods.FileExtractor;

/// <summary>
/// Variation of <see cref="TemporaryFileManager"/> with support for injecting settings via DI.
/// </summary>
internal class TemporaryFileManagerEx : TemporaryFileManager
{
    public TemporaryFileManagerEx(IFileExtractorSettings settings) : base(AbsolutePath.FromFullPath(settings.TempFolderLocation)) { }
}
