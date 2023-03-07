using BenchmarkDotNet.Attributes;
using NexusMods.Benchmarks.Interfaces;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.Benchmarks.Benchmarks;

[BenchmarkInfo("Hashing", "Tests the speed of our xxHash64 implementation.")]
[MemoryDiagnoser]
public class XxHash64 : IBenchmark
{
    // ReSharper disable once InconsistentNaming
    private readonly byte[] _oneGB;

    public XxHash64()
    {
        _oneGB = new byte[1024 * 1024 * 1024];
        Random.Shared.NextBytes(_oneGB);
    }

    [Params(1024, 1024 * 1024, 1024 * 1024 * 1024)]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public int Size { get; set; }

    [Benchmark]
    public void NonAsyncHash()
    {
        var algo = new xxHashAlgorithm(0);
        algo.HashBytes(_oneGB.AsSpan()[..Size]);
    }
}
