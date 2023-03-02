using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using NexusMods.Benchmarks.Interfaces;
using NexusMods.Paths.HighPerformance.Backports.System.Globalization;

namespace NexusMods.Benchmarks.Benchmarks;

[SkipLocalsInit]
[MemoryDiagnoser]
[BenchmarkInfo("Change Case", "Tests the performance of changing the case on a string.")]
public class ChangeCase : IBenchmark
{
    [Params(4, 16, 64, 256, 1024)]
    public int StringLength { get; set; }

    private static Random _random = new Random();
    private string _value;
    private string _valueEmoji;

    [GlobalSetup]
    public void Setup()
    {
        _value = RandomStringUpper(StringLength);
        _valueEmoji = RandomStringUpperWithEmoji(StringLength);
    }

    [Benchmark]
    public void Runtime_Current_NonAscii()
    {
        Span<char> buffer = stackalloc char[_valueEmoji.Length];
        MemoryExtensions.ToLowerInvariant(_valueEmoji, buffer);
    }

    [Benchmark(Baseline = true)]
    public void Runtime_Current()
    {
        Span<char> buffer = stackalloc char[_value.Length];
        MemoryExtensions.ToLowerInvariant(_value, buffer);
    }

    [Benchmark]
    public void NET8_Backport()
    {
        Span<char> buffer = stackalloc char[_value.Length];
        TextInfo.ChangeCase<TextInfo.ToLowerConversion>(_value, buffer);
    }

    [Benchmark]
    public void NET8_Backport_NonAscii()
    {
        Span<char> buffer = stackalloc char[_valueEmoji.Length];
        TextInfo.ChangeCase<TextInfo.ToLowerConversion>(_valueEmoji, buffer);
    }

    private static string RandomString(int length, string charSet)
    {
        return new string(Enumerable.Repeat(charSet, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    private static string RandomStringUpper(int length) => RandomString(length, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");

    private static string RandomStringUpperWithEmoji(int length) => RandomString(length, "ABCDEFGHIJKLMNOPQRSTUVWXYZ⚠️🚦🔺🏒😕🏞🖌🖕🌷☠⛩🍸👳🍠🚦📟💦🚏🌥🏪🌖😱");

}
