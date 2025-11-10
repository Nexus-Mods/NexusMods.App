using System.Text;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.Sdk.Hashes;

namespace NexusMods.Sdk.Tests.Hashes;

public class MultiHasherTests
{
    [Test]
    public async Task Test_Hasher()
    {
        const string input = "foo bar baz";
        var inputBytes = Encoding.ASCII.GetBytes(input);
        var stream = new MemoryStream(inputBytes);

        var multiHash = await MultiHasher.HashStream(stream);
        await Assert.That(multiHash).IsEqualTo(new MultiHash
        {
            Size = Size.FromLong(inputBytes.LongLength),
            Crc32 = Crc32Value.From(4066565729),
            Md5 = Md5Value.FromHex("AB07ACBB1E496801937ADFA772424BF7"),
            Sha1 = Sha1Value.FromHex("c7567e8b39e2428e38bf9c9226ac68de4c67dc39"),
            XxHash3 = Hash.From(0x42E71E38BDBF5020),
            XxHash64 = Hash.From(0xF8AB57241883E8FC),
            MinimalHash = Hash.From(0x42E71E38BDBF5020),
        });
    }
}
