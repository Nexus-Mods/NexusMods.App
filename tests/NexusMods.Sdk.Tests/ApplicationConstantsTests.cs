namespace NexusMods.Sdk.Tests;

public class ApplicationConstantsTests
{
    [Test]
    public async Task Test_BuildDate()
    {
        await Assert
            .That(ApplicationConstants.BuildDate)
            .IsNotEqualTo(DateTimeOffset.UnixEpoch).Because("it shouldn't be the default");
    }
}
