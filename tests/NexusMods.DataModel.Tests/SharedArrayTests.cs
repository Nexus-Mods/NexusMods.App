using FluentAssertions;
using NexusMods.DataModel.Interprocess;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests;

public class SharedArrayTests
{
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly TemporaryPath _file;

    private const int ThreadCount = 16;

    public SharedArrayTests(TemporaryFileManager temporaryFileManager)
    {
        _temporaryFileManager = temporaryFileManager;

        _file = _temporaryFileManager.CreateFile();
    }

    [Fact]
    public void CanPerformSimpleCasOperationsFromManyThreads()
    {
        var arraySize = 16;
        ulong iterations = 1024 * 16;
        using var array = new SharedArray(_file, arraySize);
        var threads = new Thread[ThreadCount];

        // Brute force CAS operations and make sure the final value is correct
        for (var i = 0; i < ThreadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                using var innerArray = new SharedArray(_file, arraySize);
                for (ulong loop = 0; loop < iterations; loop++)
                {
                    for (var idx = 0; idx < arraySize; idx++)
                    {
                        var oldValue = innerArray.Get(idx);
                        var newValue = oldValue + 1;
                        while (!innerArray.CompareAndSwap(idx, oldValue, newValue))
                        {
                            oldValue = innerArray.Get(idx);
                            newValue = oldValue + 1;
                        }
                    }
                }
            });
        }

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        for (var i = 0; i < arraySize; i++)
        {
            array.Get(1).Should().Be(ThreadCount * iterations);
        }
    }
}
