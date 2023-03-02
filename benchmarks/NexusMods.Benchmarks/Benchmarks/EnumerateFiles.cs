using BenchmarkDotNet.Attributes;
using NexusMods.Benchmarks.Interfaces;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[BenchmarkInfo("File Enumeration", "Tests the performance of fetching all files from a directory.")]
public class EnumerateFiles : IBenchmark
{
    // Placeholder path that'll probably work for people using Windows.
    // Point this at your games' directory.
    public AbsolutePath FilePath { get; } = AbsolutePath.FromFullPath(Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86))!.FullName);

    // Note: Tests are written using 'foreach' to provide more accurate memory measurements
    //       ToArray leads to extra allocation.

    [Benchmark]
    public AbsolutePath EnumerateFiles_New()
    {
        var paths = FilePath.EnumerateFiles();
        AbsolutePath result;
        foreach (var path in paths)
            result = path;

        return result;
    }

    [Benchmark]
    public AbsolutePath EnumerateFiles_Old()
    {
        var paths = OriginalImplementation.EnumerateFiles(FilePath);
        AbsolutePath result;
        foreach (var path in paths)
            result = path;

        return result;
    }

    [Benchmark]
    public AbsolutePath EnumerateDirectories_New()
    {
        var paths = FilePath.EnumerateDirectories();
        AbsolutePath result;
        foreach (var path in paths)
            result = path;

        return result;
    }

    [Benchmark]
    public AbsolutePath EnumerateDirectories_Old()
    {
        var paths = OriginalImplementation.EnumerateDirectories(FilePath);
        AbsolutePath result;
        foreach (var path in paths)
            result = path;

        return result;
    }

    [Benchmark]
    public FileEntry? EnumerateFileEntries_New()
    {
        var entries = FilePath.EnumerateFileEntries();
        FileEntry? result = default;
        foreach (var entry in entries)
            result = entry;

        return result;
    }

    [Benchmark]
    public FileEntry? EnumerateFileEntries_Old()
    {
        var entries = OriginalImplementation.EnumerateFileEntries(FilePath);
        FileEntry? result = default;
        foreach (var entry in entries)
            result = entry;

        return result;
    }

    #region Original Implementation

    private struct OriginalImplementation
    {
        public static IEnumerable<AbsolutePath> EnumerateFiles(AbsolutePath path, string pattern = "*", bool recursive = true)
        {
            return Directory.EnumerateFiles(path.GetFullPath(), pattern,
                    new EnumerationOptions()
                    {
                        AttributesToSkip = 0,
                        RecurseSubdirectories = recursive,
                        MatchType = MatchType.Win32
                    })
                .Select(file => file.ToAbsolutePath());
        }

        public static IEnumerable<AbsolutePath> EnumerateDirectories(AbsolutePath path, bool recursive = true)
        {
            if (!path.DirectoryExists())
                return Array.Empty<AbsolutePath>();

            return Directory.EnumerateDirectories(path.GetFullPath(), "*",
                    new EnumerationOptions()
                    {
                        AttributesToSkip = 0,
                        RecurseSubdirectories = recursive,
                        MatchType = MatchType.Win32
                    })
                .Select(p => p.ToAbsolutePath());
        }

        public static IEnumerable<FileEntry> EnumerateFileEntries(AbsolutePath path, string pattern = "*",
            bool recursive = true)
        {
            if (!path.DirectoryExists()) return Array.Empty<FileEntry>();
            return Directory.EnumerateFiles(path.GetFullPath(), pattern,
                    new EnumerationOptions()
                    {
                        AttributesToSkip = 0,
                        RecurseSubdirectories = recursive,
                        MatchType = MatchType.Win32
                    })
                .Select(file =>
                {
                    var absPath = file.ToAbsolutePath();
                    var info = absPath.FileInfo;
                    return new FileEntry(Path: absPath, Size: Size.From(info.Length), LastModified: info.LastWriteTimeUtc);
                });
        }
    }
    #endregion
}
