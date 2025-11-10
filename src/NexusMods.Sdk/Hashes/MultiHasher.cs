using System.IO.Hashing;
using System.Security.Cryptography;
using JetBrains.Annotations;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Sdk.Hashes;

[PublicAPI]
public static class MultiHasher
{
    private static readonly MultiHasherBuilder<Crc32Value, Crc32, Crc32Hasher, Md5Value, MD5, Md5Hasher, Sha1Value, SHA1, Sha1Hasher, Hash, XxHash3, Xx3Hasher, Hash, XxHash64, Xx64Hasher> Hasher;

    static MultiHasher()
    {
        Hasher = MultiHasherBuilder.Start()
            .AddHasher<Crc32Value, Crc32, Crc32Hasher>()
            .AddHasher<Md5Value, MD5, Md5Hasher>()
            .AddHasher<Sha1Value, SHA1, Sha1Hasher>()
            .AddHasher<Hash, XxHash3, Xx3Hasher>()
            .AddHasher<Hash, XxHash64, Xx64Hasher>();
    }

    public static async Task<MultiHash> HashStream(Stream stream, CancellationToken cancellationToken = default)
    {
        stream.Position = 0;
        var multiHash = await Hasher.HashAsync(stream, cancellationToken: cancellationToken);
        var minimalHash = await MinimalHash.HashAsync<Hash, XxHash3, Xx3Hasher>(stream, cancellationToken: cancellationToken);

        var result = new MultiHash
        {
            Crc32 = multiHash.Hash1,
            Md5 = multiHash.Hash2,
            Sha1 = multiHash.Hash3,
            XxHash3 = multiHash.Hash4,
            XxHash64 = multiHash.Hash5,

            MinimalHash = minimalHash,
            Size = Size.FromLong(stream.Length),
        };

        return result;
    }
}
