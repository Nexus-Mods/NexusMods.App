using BenchmarkDotNet.Attributes;
using NexusMods.Benchmarks.Interfaces;
using NexusMods.Paths;

namespace NexusMods.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[BenchmarkInfo("File Enumeration", "Tests the performance of fetching all files from a directory.")]
public class EnumerateFiles : IBenchmark
{
    private readonly IFileSystem _fileSystem;

    public EnumerateFiles()
    {
        _fileSystem = FileSystem.Shared;
        FilePath = _fileSystem.GetKnownPath(KnownPath.ApplicationDataDirectory);
    }

    // Placeholder path that'll probably work for people using Windows.
    // Point this at your games' directory.
    public AbsolutePath FilePath { get; }

    // Note: Tests are written using 'foreach' to provide more accurate memory measurements
    //       ToArray leads to extra allocation.

    [Benchmark]
    public AbsolutePath EnumerateFiles_New()
    {
        var paths = FilePath.EnumerateFiles();
        AbsolutePath result = default;
        foreach (var path in paths)
            result = path;

        return result;
    }

    [Benchmark]
    public AbsolutePath EnumerateFiles_Old()
    {
        var paths = OriginalImplementation.EnumerateFiles(FilePath);
        AbsolutePath result = default;
        foreach (var path in paths)
            result = path;

        return result;
    }

    [Benchmark]
    public AbsolutePath EnumerateDirectories_New()
    {
        var paths = FileSystem.Shared.EnumerateDirectories(FilePath);
        AbsolutePath result = default;
        foreach (var path in paths)
            result = path;

        return result;
    }

    [Benchmark]
    public AbsolutePath EnumerateDirectories_Old()
    {
        var paths = OriginalImplementation.EnumerateDirectories(FilePath);
        AbsolutePath result = default;
        foreach (var path in paths)
            result = path;

        return result;
    }

    [Benchmark]
    public IFileEntry? EnumerateFileEntries_New()
    {
        var entries = FilePath.EnumerateFileEntries();
        IFileEntry? result = default;
        foreach (var entry in entries)
            result = entry;

        return result;
    }

    // [Benchmark]
    // public IFileEntry? EnumerateFileEntries_Old()
    // {
    //     var entries = OriginalImplementation.EnumerateFileEntries(FilePath);
    //     IFileEntry? result = default;
    //     foreach (var entry in entries)
    //         result = entry;
    //
    //     return result;
    // }

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
                .Select(file => FileSystem.Shared.FromUnsanitizedFullPath(file));
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
                .Select(file => FileSystem.Shared.FromUnsanitizedFullPath(file));
        }

        // public static IEnumerable<FileEntry> EnumerateFileEntries(AbsolutePath path, string pattern = "*",
        //     bool recursive = true)
        // {
        //     if (!path.DirectoryExists()) return Array.Empty<FileEntry>();
        //     return Directory.EnumerateFiles(path.GetFullPath(), pattern,
        //             new EnumerationOptions()
        //             {
        //                 AttributesToSkip = 0,
        //                 RecurseSubdirectories = recursive,
        //                 MatchType = MatchType.Win32
        //             })
        //         .Select(file =>
        //         {
        //             var absPath = file.ToAbsolutePath();
        //             var info = absPath.FileInfo;
        //             return new FileEntry(Path: absPath, Size: info.Size, LastModified: info.LastWriteTimeUtc);
        //         });
        // }
    }
    #endregion
}
