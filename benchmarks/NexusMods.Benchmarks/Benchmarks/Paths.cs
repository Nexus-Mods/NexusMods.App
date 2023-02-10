using BenchmarkDotNet.Attributes;
using NexusMods.Benchmarks.Interfaces;
using NexusMods.Paths;

namespace NexusMods.Benchmarks.Benchmarks;

[BenchmarkInfo("Paths", "Comparison between our internal path routines and .NET Path.* methods")]
[MemoryDiagnoser]
public class Paths : IBenchmark
{
    private readonly string _longPath;
    private readonly string _shortPath;
    private readonly string _shortestPath;
    private readonly string _cPath;
    private readonly AbsolutePath _longPathNexus;
    private readonly string[] _paths;
    private readonly AbsolutePath _nexusShortPath;

    public Paths()
    {
        _longPath = @"c:\" + string.Join(@"\", Enumerable.Range(0, 20).Select(x => $"path_{x}"));
        _shortPath = @"c:\foo\bar\baz";
        _shortestPath = @"c:\foo";
        _nexusShortPath = _shortPath.ToAbsolutePath();
        _cPath = @"c:\";

        _paths = new[] { _longPath, _shortPath, _shortestPath, _cPath };
    }

    public IEnumerable<(string StringPath, AbsolutePath AbsolutePath)> AllPaths =>
        _paths.Select(p => (p, p.ToAbsolutePath()));
    
    [ParamsSource(nameof(AllPaths))]
    public (string StringPath, AbsolutePath AbsolutePath) CurrentPath { get; set; }

    [Benchmark]
    public void SystemGetExtension()
    {
        Path.GetExtension(CurrentPath.StringPath);
    }

    [Benchmark]
    public void NexusGetExtension()
    {
        var ext = CurrentPath.AbsolutePath.Extension;
    }

    [Benchmark]
    public void SystemChangeExtension()
    {
        Path.ChangeExtension(CurrentPath.StringPath, ".zip");
    }

    [Benchmark]
    public void NexusChangeExtension()
    {
        CurrentPath.AbsolutePath.ReplaceExtension(Ext.Zip);
    }

    [Benchmark]
    public void SystemGetParent()
    {
        Path.GetDirectoryName(CurrentPath.StringPath);
    }

    [Benchmark]
    public void NexusGetParent()
    {
        var _ = CurrentPath.AbsolutePath.Parent;
    }

    [Benchmark]
    public void SystemFileExists()
    {
        File.Exists(CurrentPath.StringPath);
    }

    [Benchmark]
    public void NexusFileExists()
    {
        var _ = CurrentPath.AbsolutePath.FileExists;
    }

    [Benchmark]
    public void SystemJoinSmall()
    {
        Path.Combine(CurrentPath.StringPath, "foo");
    }

    [Benchmark]
    public void NexusJoinSmall()
    {
        CurrentPath.AbsolutePath.Join("foo");
    }

    [Benchmark]
    public void SystemJoinLarge()
    {
        Path.Combine(CurrentPath.StringPath, "foo/bar/baz/qux/qax");
    }

    [Benchmark]
    public void NexusJoinLarge()
    {
        CurrentPath.AbsolutePath.Join("foo/bar/baz/quz/qax");
    }

    [Benchmark]
    public void SystemHash()
    {
        CurrentPath.StringPath.GetHashCode();
    }

    [Benchmark]
    public void NexusHash()
    {
        CurrentPath.AbsolutePath.GetHashCode();
    }

    [Benchmark]
    public void SystemEquals()
    {
        CurrentPath.StringPath.Equals(_shortPath);
    }

    [Benchmark]
    public void NexusEquals()
    {
        CurrentPath.AbsolutePath.Equals(_nexusShortPath);
    }
}