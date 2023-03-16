using System.IO.Enumeration;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths;

internal sealed class DirectoriesEnumerator : FileSystemEnumerator<string>
{
    public string CurrentDirectory { get; private set; }

    private readonly string _pattern;
    private readonly EnumerationOptions _options;

    public DirectoriesEnumerator(string directory, string pattern, EnumerationOptions options) : base(directory, options)
    {
        CurrentDirectory = directory;
        _pattern = pattern;
        _options = options;
    }

    protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
        => entry.IsDirectory && EnumeratorHelpers.MatchesPattern(_pattern, entry.FileName, _options.MatchType);

    protected override string TransformEntry(ref FileSystemEntry entry)
    {
        CurrentDirectory = entry.Directory.ToString();
        return entry.FileName.ToString();
    }
}
