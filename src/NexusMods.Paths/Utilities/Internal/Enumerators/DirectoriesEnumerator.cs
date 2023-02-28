using System.IO.Enumeration;

namespace NexusMods.Paths.Utilities.Internal.Enumerators;

internal sealed unsafe class DirectoriesEnumerator : FileSystemEnumerator<string>
{
    public string? CurrentDirectory { get; private set; }
    
    private readonly string _pattern;
    private readonly EnumerationOptions _options;

    public DirectoriesEnumerator(string directory, string pattern, EnumerationOptions options) : base(directory, options)
    {
        _pattern = pattern;
        _options = options;
    }

    protected override void OnDirectoryFinished(ReadOnlySpan<char> directory) => CurrentDirectory = null;

    protected override bool ShouldIncludeEntry(ref System.IO.Enumeration.FileSystemEntry entry) => 
        entry.IsDirectory && Common.MatchesPattern(_pattern, entry.FileName, _options);

    protected override string TransformEntry(ref System.IO.Enumeration.FileSystemEntry entry)
    {
        CurrentDirectory ??= entry.Directory.ToString();
        return entry.FileName.ToString();
    }
}