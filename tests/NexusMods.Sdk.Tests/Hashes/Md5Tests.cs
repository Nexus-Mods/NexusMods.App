using System.Text;
using NexusMods.Sdk.Hashes;

namespace NexusMods.Sdk.Tests.Hashes;

[InheritsTests]
public class Md5Tests : HasherTestBase<Md5Value, Md5Hasher>
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

    [Test]
    [Arguments("foo bar baz", "ab07acbb1e496801937adfa772424bf7")]
    public async Task Test_StreamingHasher(string input, string expectedHex)
    {
        var expected = Md5Value.FromHex(expectedHex);

        var inputBytes = Encoding.ASCII.GetBytes(input);
        var stream = new MemoryStream(inputBytes);

        var actual = await Md5Hasher.HashAsync(stream);
        await Assert.That(actual).IsEqualTo(expected);
    }

    public override IEnumerable<(string input, Md5Value expected)> GetTestData()
    {
        yield return ("foo bar baz", Md5Value.FromHex("ab07acbb1e496801937adfa772424bf7"));
    }
}
