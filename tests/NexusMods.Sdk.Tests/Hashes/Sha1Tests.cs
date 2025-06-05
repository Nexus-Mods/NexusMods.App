using NexusMods.Sdk.Hashes;

namespace NexusMods.Sdk.Tests.Hashes;

public class Sha1Tests
{
    [Test]
    [Arguments("40C47F6AE2760000140000000000000030C67F6A")]
    public async ValueTask Test_Hex(string input)
    {
        var expectedBytes = Convert.FromHexString(input);

        var value = Sha1Value.FromHex(input);
        var actualBytes = value.AsSpan().ToArray();

        await Assert.That(actualBytes.SequenceEqual(expectedBytes)).IsTrue();
    }
}
