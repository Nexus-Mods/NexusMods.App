using BenchmarkDotNet.Attributes;
using NexusMods.Benchmarks.Interfaces;
using NexusMods.Paths.HighPerformance.Backports.System.Globalization;

namespace NexusMods.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[BenchmarkInfo("Change Case + Ordinal vs OrdinalIgnoreCase", "Compares Change Case + Ordinal vs OrdinalIgnoreCase")]
public class ChangeCaseVsOrdinalIgnoreCase : IBenchmark
{
    [Params(4, 16, 64, 256, 1024)]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public int StringLength { get; set; }

    private string _value1 = null!;
    private string _value2 = null!;

    [GlobalSetup]
    public void Setup()
    {
        _value1 = RandomStringUpper(StringLength);
        _value2 = _value1 + "_";
    }

    [Benchmark(Baseline = true)]
    public bool WithOrdinalIgnoreCase()
    {
        return string.Equals(_value1, _value2, StringComparison.OrdinalIgnoreCase);
    }

    [Benchmark]
    public bool WithChangeCaseOrdinal()
    {
        var span1 = _value1.Length > 512
            ? GC.AllocateUninitializedArray<char>(_value1.Length)
            : stackalloc char[_value1.Length];
        TextInfo.ChangeCase<TextInfo.ToLowerConversion>(_value1, span1);

        var span2 = _value1.Length > 512
            ? GC.AllocateUninitializedArray<char>(_value2.Length)
            : stackalloc char[_value2.Length];
        TextInfo.ChangeCase<TextInfo.ToLowerConversion>(_value2, span2);

        return span1.SequenceEqual(span2);
    }

    private static string RandomString(int length, string charSet)
    {
        return new string(Enumerable.Repeat(charSet, length)
            .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }

    private static string RandomStringUpper(int length) => RandomString(length, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
}
