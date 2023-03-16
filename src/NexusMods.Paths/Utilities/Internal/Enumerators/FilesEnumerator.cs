using System.IO.Enumeration;

namespace NexusMods.Paths.Utilities.Internal.Enumerators;

internal sealed class FilesEnumerator : FileSystemEnumerator<FilesEnumeratorEntry>
{
    public string CurrentDirectory { get; private set; }

    private readonly string _pattern;
    private readonly EnumerationOptions _options;

    public FilesEnumerator(string directory, string pattern, EnumerationOptions options) : base(directory, options)
    {
        CurrentDirectory = directory;
        _pattern = pattern;
        _options = options;
    }

    protected override bool ShouldIncludeEntry(ref FileSystemEntry entry) =>
        Common.MatchesPattern(_pattern, entry.FileName, _options);

    protected override FilesEnumeratorEntry TransformEntry(ref FileSystemEntry entry)
    {
        CurrentDirectory = entry.Directory.ToString();
        return new FilesEnumeratorEntry(entry.FileName.ToString(), entry.IsDirectory);
    }
}

internal record struct FilesEnumeratorEntry(string FileName, bool IsDirectory);
