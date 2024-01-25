using BenchmarkDotNet.Attributes;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.Benchmarks.Interfaces;

namespace NexusMods.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[BenchmarkInfo("Id.ToString", "Benchmarking the Id.ToString methods of our IId implementations")]
public class IdToString : IBenchmark
{
    private readonly AId _id = new Id64(EntityCategory.Loadouts, ulong.MaxValue);

    [Benchmark(Baseline = true)]
    public string Old()
    {
        Span<byte> span = stackalloc byte[_id.SpanSize];
        _id.ToSpan(span);
        return $"{_id.Category}-{Convert.ToHexString(span)}";
    }

    [Benchmark]
    public string New_WithEnumExtensions()
    {
        Span<byte> span = stackalloc byte[_id.SpanSize];
        _id.ToSpan(span);
        return $"{_id.Category.ToStringFast()}-{Convert.ToHexString(span)}";
    }
}
