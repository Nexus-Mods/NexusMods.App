using BenchmarkDotNet.Attributes;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.Hashing.Benchmarks;

[MemoryDiagnoser]
public class XxHash64
{
    private readonly byte[] _oneGB;

    public XxHash64()
    {
        _oneGB = new byte[1024 * 1024 * 1024];
        Random.Shared.NextBytes(_oneGB);
    }
    
    [Params(1024, 1024 * 1024, 1024 * 1024 * 1024)]
    public int Size { get; set; }
    
    [Benchmark]
    public void NonAsyncHash()
    {
        var algo = new xxHashAlgorithm(0);
        algo.HashBytes(_oneGB.AsSpan()[..Size]);
    }
}

/*
// Before Optimization
|       Method |       Size |            Mean |           Error |          StdDev | Allocated |
|------------- |----------- |----------------:|----------------:|----------------:|----------:|
| NonAsyncHash |       1024 |        108.6 ns |         1.76 ns |         1.88 ns |         - |
| NonAsyncHash |    1048576 |     80,623.2 ns |     1,572.60 ns |     1,811.01 ns |         - |
| NonAsyncHash | 1073741824 | 86,212,807.1 ns | 1,723,623.96 ns | 1,770,034.91 ns |     120 B |

// After switching to const instead of a readonly list
|       Method |       Size |             Mean |            Error |           StdDev | Allocated |
|------------- |----------- |-----------------:|-----------------:|-----------------:|----------:|
| NonAsyncHash |       1024 |         85.76 ns |         0.838 ns |         0.654 ns |         - |
| NonAsyncHash |    1048576 |     79,198.56 ns |     1,526.452 ns |     1,817.132 ns |         - |
| NonAsyncHash | 1073741824 | 85,147,219.00 ns | 1,683,996.950 ns | 1,939,293.392 ns |     120 B |

// After removing block size check (switched to #if DEBUG)

|       Method |       Size |             Mean |            Error |           StdDev | Allocated |
|------------- |----------- |-----------------:|-----------------:|-----------------:|----------:|
| NonAsyncHash |       1024 |         85.78 ns |         0.323 ns |         0.270 ns |         - |
| NonAsyncHash |    1048576 |     79,632.00 ns |     1,582.805 ns |     2,001.741 ns |         - |
| NonAsyncHash | 1073741824 | 84,662,808.00 ns | 1,363,769.258 ns | 1,275,670.610 ns |     120 B |

// After switching to BitOperations.RotateLeft
|       Method |       Size |             Mean |            Error |           StdDev | Allocated |
|------------- |----------- |-----------------:|-----------------:|-----------------:|----------:|
| NonAsyncHash |       1024 |         87.28 ns |         1.664 ns |         1.709 ns |         - |
| NonAsyncHash |    1048576 |     79,263.84 ns |     1,547.129 ns |     1,781.676 ns |         - |
| NonAsyncHash | 1073741824 | 84,935,244.55 ns | 1,632,233.187 ns | 2,004,528.937 ns |     120 B |

// After switching to unsafe code

|       Method |       Size |             Mean |            Error |           StdDev | Allocated |
|------------- |----------- |-----------------:|-----------------:|-----------------:|----------:|
| NonAsyncHash |       1024 |         55.95 ns |         0.979 ns |         0.916 ns |         - |
| NonAsyncHash |    1048576 |     50,299.26 ns |       276.969 ns |       231.282 ns |         - |
| NonAsyncHash | 1073741824 | 56,262,583.64 ns | 1,106,632.413 ns | 1,359,043.985 ns |      60 B |

// After inlining constants

|       Method |       Size |             Mean |            Error |           StdDev | Allocated |
|------------- |----------- |-----------------:|-----------------:|-----------------:|----------:|
| NonAsyncHash |       1024 |         55.13 ns |         1.088 ns |         1.018 ns |         - |
| NonAsyncHash |    1048576 |     49,998.33 ns |       398.988 ns |       333.173 ns |         - |
| NonAsyncHash | 1073741824 | 55,888,864.21 ns | 1,116,668.022 ns | 1,241,173.548 ns |      60 B |


*/