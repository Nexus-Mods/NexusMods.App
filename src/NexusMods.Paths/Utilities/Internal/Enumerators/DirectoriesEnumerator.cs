using System.IO.Enumeration;

namespace NexusMods.Paths.Utilities.Internal.Enumerators;

internal sealed class DirectoriesEnumerator : FileSystemEnumerator<string>
{
    public string Directory { get; }
    
    private readonly string _pattern;
    private readonly EnumerationOptions _options;

    public DirectoriesEnumerator(string directory, string pattern, EnumerationOptions options) : base(directory, options)
    {
        Directory = directory;
        _pattern = pattern;
        _options = options;
    }

    protected override bool ShouldIncludeEntry(ref System.IO.Enumeration.FileSystemEntry entry) => 
        entry.IsDirectory && Common.MatchesPattern(_pattern, entry.FileName, _options);

    protected override string TransformEntry(ref System.IO.Enumeration.FileSystemEntry entry) => entry.FileName.ToString();
}