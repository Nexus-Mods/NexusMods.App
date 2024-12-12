using System.Diagnostics;
using System.Text;
using FluentAssertions;
using NexusMods.Abstractions.IO.ChunkedStreams;
using NexusMods.Hashing.xxHash3;
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
        _hash = _ms.ToArray().AsSpan().xxHash3();
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
    public async Task CanCopyLargeStream()
    {
        var chunked = new ChunkedStream<ChunkedMemoryStream>(new ChunkedMemoryStream(_ms, 1024));
        (await chunked.HashingCopyAsync(Stream.Null, CancellationToken.None)).Should().Be(_hash);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(11)] // Same as the input data
    [InlineData(16)]
    [InlineData(32)]
    public async Task CanCopySmallStreamAsync(int chunkSize)
    {
        var ms = new MemoryStream();
        ms.Position = 0;
        ms.Write("Hello World"u8);
        var chunked = new ChunkedStream<ChunkedMemoryStream>(new ChunkedMemoryStream(ms, chunkSize));

        var outStream = new MemoryStream();
        await chunked.CopyToAsync(outStream);
        outStream.Length.Should().Be(ms.Length);
        Encoding.UTF8.GetString(outStream.ToArray()).Should().Be("Hello World");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(11)] // Same as the input data
    [InlineData(16)]
    [InlineData(32)]
    public void CanCopySmallStream(int chunkSize)
    {
        var ms = new MemoryStream();
        ms.Position = 0;
        ms.Write("Hello World"u8);
        var chunked = new ChunkedStream<ChunkedMemoryStream>(new ChunkedMemoryStream(ms, chunkSize));

        var outStream = new MemoryStream();
        chunked.CopyTo(outStream);
        outStream.Length.Should().Be(ms.Length);
        Encoding.UTF8.GetString(outStream.ToArray()).Should().Be("Hello World");
    }

    [Theory]
    [InlineData(0, 128)]
    [InlineData(3, 128)]
    [InlineData(1024, 2)]
    [InlineData(1024 * 1024, 1024)]
    public async Task CanSeekAsync(int position, int size)
    {
        var chunked = new ChunkedStream<ChunkedMemoryStream>(new ChunkedMemoryStream(_ms, 16));
        var buffer1 = new byte[size];
        chunked.Position = position;
        await chunked.ReadExactlyAsync(buffer1);
        _ms.Position = position;
        var buffer2 = new byte[size];
        await _ms.ReadExactlyAsync(buffer2);
        buffer1.Should().BeEquivalentTo(buffer2);
    }

    [Theory]
    [InlineData(0, 128)]
    [InlineData(3, 128)]
    [InlineData(1024, 2)]
    [InlineData(1024 * 1024, 1024)]
    public void CanSeek(int position, int size)
    {
        var chunked = new ChunkedStream<ChunkedMemoryStream>(new ChunkedMemoryStream(_ms, 16));
        var buffer1 = new byte[size];
        chunked.Position = position;
        chunked.ReadExactly(buffer1);
        _ms.Position = position;
        var buffer2 = new byte[size];
        _ms.ReadExactly(buffer2);
        buffer1.Should().BeEquivalentTo(buffer2);
    }


    [Fact]
    public async Task CopyingAfterEndOfStreamReturnsZero()
    {
        var ms = new MemoryStream();
        ms.Write(new byte[16]);
        ms.Position = 0;
        var chunked = new ChunkedStream<ChunkedMemoryStream>(new ChunkedMemoryStream(ms, 16));
        chunked.Read(new byte[16]).Should().Be(16);
        chunked.Read(new byte[16]).Should().Be(0);
    }


    class ChunkedMemoryStream : IChunkedStreamSource
    {
        private readonly MemoryStream _ms;
        private readonly int _chunkSize;
        private (ulong Offset, byte[] Data)[] _chunks;

        public ChunkedMemoryStream(MemoryStream ms, int chunkSize)
        {
            _chunkSize = chunkSize;
            _ms = ms;

            
            List<(ulong Offset, byte[] Data)> chunks = [];
            var offset = 0;
            var done = false;
            while (!done)
            {
                var size = Random.Shared.Next(128, 1024);
                if (offset + size > ms.Length)
                {
                    size = (int)(ms.Length - offset);
                    done = true;
                }
                _ms.Position = offset;
                var buffer = new byte[size];
                _ms.ReadExactly(buffer);
                chunks.Add(((ulong)offset, buffer));
                offset += size;
            }
            _chunks = chunks.ToArray();
        }

        public Size Size => Size.FromLong(_ms.Length);
        public ulong ChunkCount => (ulong)_chunks.Length;
        public ulong GetOffset(ulong chunkIndex) => _chunks[chunkIndex].Offset;
        public int GetChunkSize(ulong chunkIndex) => _chunks[chunkIndex].Data.Length;

        public async Task ReadChunkAsync(Memory<byte> buffer, ulong chunkIndex, CancellationToken token = default)
        {
            await Task.Yield();
            Debug.Assert(buffer.Length == _chunks[chunkIndex].Data.Length);
            _chunks[chunkIndex].Data.CopyTo(buffer.Span);
        }

        public void ReadChunk(Span<byte> buffer, ulong chunkIndex)
        {
            Debug.Assert(buffer.Length == _chunks[chunkIndex].Data.Length);
            _chunks[chunkIndex].Data.CopyTo(buffer);
        }
    }
}
