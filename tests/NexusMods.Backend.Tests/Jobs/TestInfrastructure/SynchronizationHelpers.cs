using FluentAssertions;
using NexusMods.Sdk.Jobs;

namespace NexusMods.Jobs.Tests.TestInfrastructure;

public static class SynchronizationHelpers
{
    public static async Task WaitForJobState(IJob job, JobStatus expectedStatus, TimeSpan timeout)
    {
        var startTime = DateTime.UtcNow;
        while (job.Status != expectedStatus && DateTime.UtcNow - startTime < timeout)
            await Task.Delay(10);
        
        job.Status.Should().Be(expectedStatus, $"Job should have reached {expectedStatus} within {timeout}");
    }
}
