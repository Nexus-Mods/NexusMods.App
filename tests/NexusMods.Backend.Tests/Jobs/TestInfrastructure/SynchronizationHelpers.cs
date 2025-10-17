using NexusMods.Sdk.Jobs;
using Assert = TUnit.Assertions.Assert;

namespace NexusMods.Backend.Tests.Jobs.TestInfrastructure;

public static class SynchronizationHelpers
{
    public static async Task WaitForJobState(IJob job, JobStatus expectedStatus, TimeSpan timeout)
    {
        var startTime = DateTime.UtcNow;
        while (job.Status != expectedStatus && DateTime.UtcNow - startTime < timeout)
            await Task.Delay(10);
        
        await Assert.That(job.Status).IsEqualTo(expectedStatus, $"Job should have reached {expectedStatus} within {timeout}");
    }
}
