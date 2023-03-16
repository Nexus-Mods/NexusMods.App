using System.IO.Enumeration;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths;

internal sealed class FilesEnumeratorEx : FileSystemEnumerator<FilesEnumeratorExEntry>
{
    public string CurrentDirectory { get; private set; }

    private readonly string _pattern;
    private readonly EnumerationOptions _options;

    public FilesEnumeratorEx(string directory, string pattern, EnumerationOptions options) : base(directory, options)
    {
        CurrentDirectory = directory;
        _pattern = pattern;
        _options = options;
    }

    protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
        => EnumeratorHelpers.MatchesPattern(_pattern, entry.FileName, _options.MatchType);

    protected override FilesEnumeratorExEntry TransformEntry(ref FileSystemEntry entry)
    {
        CurrentDirectory = entry.Directory.ToString();
        return new FilesEnumeratorExEntry(entry.FileName.ToString(), Size.From(entry.Length), entry.LastWriteTimeUtc.DateTime, entry.IsDirectory);
    }
}

internal record struct FilesEnumeratorExEntry(string FileName, Size Size, DateTime LastModified, bool IsDirectory);
