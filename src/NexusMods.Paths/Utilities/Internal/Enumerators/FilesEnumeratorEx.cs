using System.IO.Enumeration;

namespace NexusMods.Paths.Utilities.Internal.Enumerators;

internal sealed class FilesEnumeratorEx : FileSystemEnumerator<FilesEnumeratorExEntry>
{
    public string Directory { get; }
    
    private readonly string _pattern;
    private readonly EnumerationOptions _options;

    public FilesEnumeratorEx(string directory, string pattern, EnumerationOptions options) : base(directory, options)
    {
        Directory = directory;
        _pattern = pattern;
        _options = options;
    }

    protected override bool ShouldIncludeEntry(ref FileSystemEntry entry) => 
        Common.MatchesPattern(_pattern, entry.FileName, _options);

    protected override FilesEnumeratorExEntry TransformEntry(ref FileSystemEntry entry) 
        => new (entry.FileName.ToString(), Size.From(entry.Length), entry.LastWriteTimeUtc.DateTime, entry.IsDirectory);
}

internal record struct FilesEnumeratorExEntry(string FileName, Size Size, DateTime LastModified, bool IsDirectory);