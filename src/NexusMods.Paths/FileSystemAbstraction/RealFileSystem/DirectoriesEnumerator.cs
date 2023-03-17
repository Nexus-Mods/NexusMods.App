using System.IO.Enumeration;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths;

internal sealed class DirectoriesEnumerator : FileSystemEnumerator<string>
{
    private string? _currentDirectory;
    public string CurrentDirectory => _currentDirectory ?? _startDirectory;

    private readonly string _startDirectory;
    private readonly string _pattern;
    private readonly EnumerationOptions _options;

    public DirectoriesEnumerator(string directory, string pattern, EnumerationOptions options) : base(directory, options)
    {
        _startDirectory = directory;
        _pattern = pattern;
        _options = options;
    }

    protected override void OnDirectoryFinished(ReadOnlySpan<char> directory)
        => _currentDirectory = null;

    protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
        => entry.IsDirectory && EnumeratorHelpers.MatchesPattern(_pattern, entry.FileName, _options.MatchType);

    protected override string TransformEntry(ref FileSystemEntry entry)
    {
        _currentDirectory ??= entry.Directory.ToString();
        return entry.FileName.ToString();
    }
}
