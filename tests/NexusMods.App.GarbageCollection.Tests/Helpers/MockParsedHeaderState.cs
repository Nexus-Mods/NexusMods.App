using NexusMods.App.GarbageCollection.Interfaces;
using NexusMods.App.GarbageCollection.Structs;
namespace NexusMods.App.GarbageCollection.Tests.Helpers;

internal class MockParsedHeaderState : ICanProvideFileHashes
{
    private readonly Hash[] _hashes;

    public MockParsedHeaderState(params Hash[] hashes) => _hashes = hashes;

    public Span<Hash> GetFileHashes() => _hashes;
}
