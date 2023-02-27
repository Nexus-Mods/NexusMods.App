using FluentAssertions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Hashing.xxHash64.Tests;

public class HashTests
{
    private static string _knownString = "Something clever should go here";
    private static Hash _knownHash = Hash.FromHex("F4C92BE058F432D0");
    
    [Fact]
    public void CanConvertHashBetweenFormats()
    {
        var hash = Hash.FromULong(0xDEADBEEFDECAFBAD);
        
        ((ulong)hash).Should().Be(0xDEADBEEFDECAFBAD);
        hash.ToHex().Should().Be("DEADBEEFDECAFBAD");

        Hash.FromHex("DEADBEEFDECAFBAD").Should().Be(hash);

        Hash.FromLong((long)_knownHash).Should().Be(_knownHash);
        Hash.FromULong((ulong)_knownHash).Should().Be(_knownHash);
        _knownHash.ToString().Should().Be("0x" + _knownHash.ToHex());
    }

    [Fact]
    public void CanCompareHashes()
    {
        var hash1 = Hash.FromULong(0);
        var hash2 = Hash.FromULong(1);
        var hash3 = Hash.FromULong(2);

        hash1.Should().BeLessThan(hash2);
        hash2.Should().BeLessThan(hash3);

        hash1.Should().Be(hash1);
        hash1.Should().NotBe(hash2);

        hash1.Should().BeRankedEquallyTo(hash1);
        
        Assert.True(hash1 != hash2);
        Assert.False(hash1 == hash2);
    }

    [Fact]
    public void CanHashStrings()
    {
        _knownString.XxHash64()
            .Should().Be(_knownHash);
    }

    [Fact]
    public async Task CanHashFile()
    {
        var file = KnownFolders.CurrentDirectory.CombineUnchecked($"tempFile{Guid.NewGuid()}");
        await file.WriteAllTextAsync(_knownString);
        (await file.XxHash64()).Should().Be(_knownHash);
    }

    [Fact]
    public async Task CanHashLargeFile()
    {
        var file = KnownFolders.CurrentDirectory.CombineUnchecked($"tempFile{Guid.NewGuid()}");
        var emptyArray = new byte[1024 * 1024 * 10];
        for (var x = 0; x < emptyArray.Length; x++)
        {
            emptyArray[x] = (byte)(x % 256);
        }
        await file.WriteAllBytesAsync(emptyArray);
        (await file.XxHash64()).Should().NotBe(Hash.FromULong(0xf4c92be058f432d0));
        file.Delete();
    }
}