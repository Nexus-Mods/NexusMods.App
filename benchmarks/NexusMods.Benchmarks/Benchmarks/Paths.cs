using BenchmarkDotNet.Attributes;
using NexusMods.Benchmarks.Interfaces;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.Benchmarks.Benchmarks;

[BenchmarkInfo("Paths", "Comparison between our internal path routines and their closest .NET Path.* methods")]
[MemoryDiagnoser]
public class Paths : IBenchmark
{
    private readonly string _shortPath;
    private readonly string[] _paths;
    private readonly AbsolutePath _nexusShortPath;

    public Paths()
    {
        var longPath = @"c:\" + string.Join(@"\", Enumerable.Range(0, 20).Select(x => $"path_{x}"));
        var shortestPath = @"c:\foo";
        _shortPath = @"c:\foo\bar\baz";
        _nexusShortPath = FileSystem.Shared.FromUnsanitizedFullPath(_shortPath);
        var cPath = @"c:\";

        _paths = new[] { longPath, _shortPath, shortestPath, cPath };
    }

    public IEnumerable<(string StringPath, AbsolutePath AbsolutePath)> AllPaths =>
        _paths.Select(p => (p, FileSystem.Shared.FromUnsanitizedFullPath(p)));

    [ParamsSource(nameof(AllPaths))]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public (string StringPath, AbsolutePath AbsolutePath) CurrentPath { get; set; }

    [Benchmark]
    public string SystemGetExtension()
    {
        return Path.GetExtension(CurrentPath.StringPath);
    }

    [Benchmark]
    public Extension NexusGetExtension()
    {
        return CurrentPath.AbsolutePath.Extension;
    }

    [Benchmark]
    public string SystemChangeExtension()
    {
        return Path.ChangeExtension(CurrentPath.StringPath, ".zip");
    }

    [Benchmark]
    public AbsolutePath NexusChangeExtension()
    {
        return CurrentPath.AbsolutePath.ReplaceExtension(KnownExtensions.Zip);
    }

    [Benchmark]
    public string SystemGetParent()
    {
        return Path.GetDirectoryName(CurrentPath.StringPath)!;
    }

    [Benchmark]
    public AbsolutePath NexusGetParent()
    {
        return CurrentPath.AbsolutePath.Parent;
    }

    [Benchmark]
    public bool SystemFileExists()
    {
        return File.Exists(CurrentPath.StringPath);
    }

    [Benchmark]
    public bool NexusFileExists()
    {
        return CurrentPath.AbsolutePath.FileExists;
    }

    [Benchmark]
    public string SystemJoinSmall()
    {
        return Path.Combine(CurrentPath.StringPath, "foo");
    }

    [Benchmark]
    public AbsolutePath NexusJoinSmall()
    {
        // Not a fair test here since one is a string concat; other includes
        // normalization to OS path.
        return CurrentPath.AbsolutePath.Combine("foo");
    }

    [Benchmark]
    public string SystemJoinLarge()
    {
        return Path.Combine(CurrentPath.StringPath, "foo/bar/baz/qux/qax");
    }

    [Benchmark]
    public AbsolutePath NexusJoinLarge()
    {
        return CurrentPath.AbsolutePath.Combine("foo/bar/baz/quz/qax");
    }

    [Benchmark]
    public int SystemHash()
    {
        return CurrentPath.StringPath.GetHashCode();
    }

    [Benchmark]
    public int NexusHash()
    {
        return CurrentPath.AbsolutePath.GetHashCode();
    }

    [Benchmark]
    public bool SystemEquals()
    {
        return CurrentPath.StringPath.Equals(_shortPath);
    }

    [Benchmark]
    public bool NexusEquals()
    {
        return CurrentPath.AbsolutePath.Equals(_nexusShortPath);
    }
}
