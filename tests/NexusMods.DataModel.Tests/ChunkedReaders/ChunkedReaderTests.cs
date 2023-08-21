using System.Text;
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
        _hash = _ms.ToArray().AsSpan().XxHash64();
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
        var chunked = new ChunkedStream(new ChunkedMemoryStream(_ms, 1024));
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
        var chunked = new ChunkedStream(new ChunkedMemoryStream(ms, chunkSize));

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
        var chunked = new ChunkedStream(new ChunkedMemoryStream(ms, chunkSize));

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
        var chunked = new ChunkedStream(new ChunkedMemoryStream(_ms, 16));
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
        var chunked = new ChunkedStream(new ChunkedMemoryStream(_ms, 16));
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
        var chunked = new ChunkedStream(new ChunkedMemoryStream(ms, 16));
        chunked.Read(new byte[16]).Should().Be(16);
        chunked.Read(new byte[16]).Should().Be(0);
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
        public async Task ReadChunkAsync(Memory<byte> buffer, ulong chunkIndex, CancellationToken token = default)
        {
            var offset = chunkIndex * (ulong)_chunkSize;
            _ms.Position = (long)offset;
            await _ms.ReadAsync(buffer, token);
        }

        public void ReadChunk(Span<byte> buffer, ulong chunkIndex)
        {
            var offset = chunkIndex * (ulong)_chunkSize;
            _ms.Position = (long)offset;
            _ms.Read(buffer);
        }
    }
}
