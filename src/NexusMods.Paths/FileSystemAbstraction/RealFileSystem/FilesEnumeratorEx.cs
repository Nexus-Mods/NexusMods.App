using System.IO.Enumeration;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths;

internal sealed class FilesEnumeratorEx : FileSystemEnumerator<FilesEnumeratorExEntry>
{
    private string? _currentDirectory;
    public string CurrentDirectory => _currentDirectory ?? _startDirectory;

    private readonly string _startDirectory;

    private readonly string _pattern;
    private readonly EnumerationOptions _options;
    private readonly IOSInformation _os;

    public FilesEnumeratorEx(string directory, string pattern, EnumerationOptions options, IOSInformation os) : base(directory, options)
    {
        _startDirectory = directory;
        _pattern = pattern;
        _options = options;
        _os = os;
    }

    protected override void OnDirectoryFinished(ReadOnlySpan<char> directory)
        => _currentDirectory = null;

    protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
        => EnumeratorHelpers.MatchesPattern(_pattern, entry.FileName, _options.MatchType);

    protected override FilesEnumeratorExEntry TransformEntry(ref FileSystemEntry entry)
    {
        _currentDirectory ??= PathHelpers.Sanitize(entry.Directory, _os);
        return new FilesEnumeratorExEntry(PathHelpers.Sanitize(entry.FileName, _os), Size.FromLong(entry.Length), entry.LastWriteTimeUtc.DateTime, entry.IsDirectory);
    }
}

internal record struct FilesEnumeratorExEntry(string FileName, Size Size, DateTime LastModified, bool IsDirectory);
