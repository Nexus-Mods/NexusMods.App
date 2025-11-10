using NexusMods.Sdk.Hashes;

namespace NexusMods.Sdk.Tests.Hashes;

[InheritsTests]
public class Sha1Tests : HasherTestBase<Sha1Value, Sha1Hasher>
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

    public override IEnumerable<(string input, Sha1Value expected)> GetTestData()
    {
        yield return ("foo bar baz", Sha1Value.FromHex("c7567e8b39e2428e38bf9c9226ac68de4c67dc39"));
    }
}
