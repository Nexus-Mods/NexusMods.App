using System.IO.Enumeration;

namespace NexusMods.Paths.Utilities.Internal.Enumerators;

internal sealed class FilesEnumeratorEx : FileSystemEnumerator<FilesEnumeratorExEntry>
{
    public string? CurrentDirectory { get; private set; }

    private readonly string _pattern;
    private readonly EnumerationOptions _options;

    public FilesEnumeratorEx(string directory, string pattern, EnumerationOptions options) : base(directory, options)
    {
        _pattern = pattern;
        _options = options;
    }

    protected override void OnDirectoryFinished(ReadOnlySpan<char> directory) => CurrentDirectory = null;

    protected override bool ShouldIncludeEntry(ref FileSystemEntry entry) =>
        Common.MatchesPattern(_pattern, entry.FileName, _options);

    protected override FilesEnumeratorExEntry TransformEntry(ref FileSystemEntry entry)
    {
        CurrentDirectory ??= entry.Directory.ToString();
        return new(entry.FileName.ToString(), Size.From(entry.Length), entry.LastWriteTimeUtc.DateTime,
            entry.IsDirectory);
    }
}

internal record struct FilesEnumeratorExEntry(string FileName, Size Size, DateTime LastModified, bool IsDirectory);
