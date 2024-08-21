using System;
using NexusMods.App.GarbageCollection.Interfaces;
using NexusMods.Hashing.xxHash64;
namespace NexusMods.App.GarbageCollection.Tests.Helpers;

internal class MockParsedHeaderState : ICanProvideFileHashes<MockFileHash>
{
    private readonly MockFileHash[] _hashes;

    public MockParsedHeaderState(params MockFileHash[] hashes) => _hashes = hashes;

    public Span<MockFileHash> GetFileHashes() => _hashes;
}

public struct MockFileHash : IHaveFileHash
{
    public Hash Hash { get; set; }

    public static implicit operator MockFileHash(Hash hash) => new()
    {
        Hash = hash,
    };
}
