namespace NexusMods.Games.IntegrationTestFramework;

/// <summary>
/// Not sure why, but some of the test frameworks try to run this project and fail when no tests
/// are found
/// </summary>
public class DummyTest
{
    [Test]
    public async Task AlwaysPasses()
    {
        var x = 1;
        await Assert.That(x).IsEqualTo(x);
    }
}
