using NexusMods.Sdk.Hashes;

namespace NexusMods.Sdk.Tests.Hashes;

public class Md5Tests
{
    [Test]
    [Arguments("40C47F6AE27600001400000000000000")]
    public async ValueTask Test_Hex(string input)
    {
        var expectedBytes = Convert.FromHexString(input);

        var value = Md5Value.FromHex(input);
        var actualBytes = value.AsSpan().ToArray();

        await Assert.That(actualBytes.SequenceEqual(expectedBytes)).IsTrue();
    }
}
