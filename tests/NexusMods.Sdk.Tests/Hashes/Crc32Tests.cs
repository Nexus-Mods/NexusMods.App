using System.Text.Json;
using NexusMods.Sdk.Hashes;

namespace NexusMods.Sdk.Tests.Hashes;

public class Crc32Tests
{
    [Test]
    public async ValueTask Test_Json()
    {
        var value = Crc32Value.From(0xB0AB256A);

        var wrapper = JsonSerializer.Deserialize<Wrapper>("""{ "Value": "B0AB256A" }""");
        await Assert.That(wrapper).IsNotNull();
        await Assert.That(wrapper!.Value).IsEqualTo(value);

        var json = JsonSerializer.Serialize(wrapper);
        var wrapper2 = JsonSerializer.Deserialize<Wrapper>(json);
        await Assert.That(wrapper2).IsNotNull();
        await Assert.That(wrapper2!.Value).IsEqualTo(value);
    }
}

file class Wrapper
{
    public Crc32Value Value { get; set; }
}
