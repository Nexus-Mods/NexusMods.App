using System.Diagnostics.CodeAnalysis;
using System.Text;
using NexusMods.Sdk.Hashes;

namespace NexusMods.Sdk.Tests.Hashes;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class FNV1aTests
{
    [Test]
    [Arguments("", 0xcbf29ce484222325)]
    [Arguments("foo bar baz", 0x2ece4bef60afe2af)]
    [Arguments("Game", 0xb568a67e05a19907)]
    public async Task Test_Hash64(string input, ulong expected)
    {
        var actual = FNV1a.Hash64(input);
        await Assert.That(actual).IsEqualTo(expected);

        var bytes = Encoding.ASCII.GetBytes(input);
        var hashFromBytes = FNV1a.Hash64(bytes);
        await Assert.That(hashFromBytes).IsEqualTo(expected);
    }
    
    [Test]
    [Arguments("", 0x811c9dc5)]
    [Arguments("foo bar baz", 0xa3c6e38f)]
    [Arguments("Game", 0xc2762327)]
    public async Task Test_Hash32(string input, uint expected)
    {
        var actual = FNV1a.Hash32(input);
        await Assert.That(actual).IsEqualTo(expected);

        var bytes = Encoding.ASCII.GetBytes(input);
        var hashFromBytes = FNV1a.Hash32(bytes);
        await Assert.That(hashFromBytes).IsEqualTo(expected);
    }

    [Test]
    [Arguments("", 0x1cd9)]
    [Arguments("foo bar baz", 0x4049)]
    [Arguments("Game", 0xe151)]
    public async Task Test_Hash16(string input, ushort expected)
    {
        var actual = FNV1a.Hash16(input);
        await Assert.That(actual).IsEqualTo(expected);

        var bytes = Encoding.ASCII.GetBytes(input);
        var hashFromBytes = FNV1a.Hash16(bytes);
        await Assert.That(hashFromBytes).IsEqualTo(expected);
    }

    [Test]
    [Arguments(0xC963, "0MBD7H9HMF", "PWXWFTVG H")]
    public async Task Test_HashCollision(ushort hash, string left, string right)
    {
        var pool = new FNV1a16Pool(nameof(Test_HashCollision));
        var actual = pool.GetOrAdd(left);
        await Assert.That(actual).IsEqualTo(hash);

        var act = () => pool.GetOrAdd(right);
        await Assert.That(act).ThrowsExactly<HashCollisionException>().WithMessage($"Hash collision detected in pool '{nameof(Test_HashCollision)}' for {hash:X} between '{right}' and '{left}'");
    }
}
