using FluentAssertions;
using NexusMods.DataModel.ChunkedReaders;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests.ChunkedReaders;

public class ChunkedReaderTests
{
    private readonly MemoryStream _ms;
    private readonly Hash _hash;

    public ChunkedReaderTests()
    {
        _ms = new MemoryStream();
        CreateData();
        _hash = _ms.GetBuffer().AsSpan().XxHash64();
    }

    private void CreateData()
    {
        var seed = Random.Shared.Next();
        var rng = new Random(seed);
        var data = new byte[921]; // Odd number to offset things a bit
        rng.NextBytes(data);
        for (var x = 0; x < 1024 * 10; x++)
        {
            _ms.Write(data);
        }
        _ms.Position = 0;
    }

    [Fact]
    public async Task CanCopyStream()
    {
        var chunked = new ChunkedStream(new ChunkedMemoryStream(_ms, 1024));
        (await chunked.HashingCopyAsync(Stream.Null, CancellationToken.None)).Should().Be(_hash);
    }


    class ChunkedMemoryStream : IChunkedReaderSource
    {
        private readonly MemoryStream _ms;
        private readonly int _chunkSize;

        public ChunkedMemoryStream(MemoryStream ms, int chunkSize)
        {
            _chunkSize = chunkSize;
            _ms = ms;

        }

        public Size Size => Size.FromLong(_ms.Length);
        public Size ChunkSize => Size.FromLong(_chunkSize);
        public ulong ChunkCount => (ulong)Math.Ceiling(_ms.Length / (double)_chunkSize);
        public Task ReadChunkAsync(Memory<byte> buffer, ulong chunkIndex, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public void ReadChunk(Span<byte> buffer, ulong chunkIndex)
        {
            var offset = chunkIndex * (ulong)_chunkSize;
            _ms.Position = (long)offset;
            _ms.Read(buffer);
        }
    }
}
