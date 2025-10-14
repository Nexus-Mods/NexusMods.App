using FluentAssertions;
using JetBrains.Annotations;
using NexusMods.Sdk.Jobs;
using Xunit;

namespace NexusMods.Jobs.Tests.Examples.BestPractices;

/// <summary>
/// Simple example of job state management pattern
/// </summary>
[PublicAPI]
public class JobStateManagementExample(IJobMonitor jobMonitor)
{
    [Fact]
    public async Task RunExample()
    {
        var job = new CounterJob { TargetCount = 5 };
        var task = jobMonitor.Begin<CounterJob, int>(job);
        
        // Access job state externally
        var state = task.Job.GetJobStateData<ICounterState>();
        state.Should().NotBeNull();
        
        var result = await task;
        result.Should().Be(5);
        
        // Verify final state
        state = task.Job.GetJobStateData<ICounterState>();
        state!.CurrentCount.Should().Be(5);
    }
}

/// <summary>
/// Public interface - subset of internal state
/// </summary>
public interface ICounterState : IPublicJobStateData
{
    int CurrentCount { get; }
}

/// <summary>
/// Internal state - includes both public and private properties
/// </summary>
internal sealed class CounterJobState : ICounterState
{
    public int CurrentCount { get; set; }              // ← Exposed
    internal DateTime StartTime { get; set; }          // ← Internal only
}

/// <summary>
/// Example job that exposes state
/// </summary>
[PublicAPI]
public record CounterJob : IJobDefinitionWithStart<CounterJob, int>
{
    public required int TargetCount { get; init; }
    
    private readonly CounterJobState _state = new();
    public IPublicJobStateData? GetJobStateData() => _state;
    
    public ValueTask<int> StartAsync(IJobContext<CounterJob> context)
    {
        _state.StartTime = DateTime.UtcNow;
        for (var x = 0; x < TargetCount; x++)
            _state.CurrentCount++;

        return ValueTask.FromResult(_state.CurrentCount);
    }
}
