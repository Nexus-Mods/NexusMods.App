using System.Diagnostics;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.Sdk.IO;

namespace NexusMods.Sdk.Tests.IO;

public class ChunkedStreamTests
{
    private static (MemoryStream, Hash) CreateData()
    {
        var ms = new MemoryStream();

        var seed = Random.Shared.Next();
        var rng = new Random(seed);
        var data = new byte[921]; // Odd number to offset things a bit
        rng.NextBytes(data);

        for (var x = 0; x < 1024 * 10; x++)
        {
            ms.Write(data);
        }

        var hash = ms.ToArray().AsSpan().xxHash3();
        return (ms, hash);
    }

    [Test]
    public async Task CanCopyLargeStream()
    {
        var (stream, expectedHash) = CreateData();
        using var ms = stream;

        var source = await ChunkedMemoryStream.Create(ms, chunkSize: 1024);
        await using var chunked = new ChunkedStream<ChunkedMemoryStream>(source);

        var actualHash = await chunked.HashingCopyAsync(Stream.Null, CancellationToken.None);
        await Assert.That(actualHash).IsEqualTo(expectedHash);
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(4)]
    [Arguments(8)]
    [Arguments(11)] // Same as the input data
    [Arguments(16)]
    [Arguments(32)]
    public async Task CanCopySmallStreamAsync(int chunkSize)
    {
        using var ms = new MemoryStream();
        ms.Write("Hello World"u8);

        var source = await ChunkedMemoryStream.Create(ms, chunkSize: 1024);
        await using var chunked = new ChunkedStream<ChunkedMemoryStream>(source);

        using var outputStream = new MemoryStream();
        await chunked.CopyToAsync(outputStream);

        await Assert.That(outputStream.Length).IsEqualTo(ms.Length);
        await Assert.That(outputStream.ToArray()).IsEquivalentTo("Hello World"u8.ToArray());
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(4)]
    [Arguments(8)]
    [Arguments(11)] // Same as the input data
    [Arguments(16)]
    [Arguments(32)]
    public async Task CanCopySmallStreamSync(int chunkSize)
    {
        using var ms = new MemoryStream();
        ms.Write("Hello World"u8);

        var source = await ChunkedMemoryStream.Create(ms, chunkSize: 1024);
        await using var chunked = new ChunkedStream<ChunkedMemoryStream>(source);

        using var outputStream = new MemoryStream();
        // ReSharper disable once MethodHasAsyncOverload
        chunked.CopyTo(outputStream);

        await Assert.That(outputStream.Length).IsEqualTo(ms.Length);
        await Assert.That(outputStream.ToArray()).IsEquivalentTo("Hello World"u8.ToArray());
    }

    [Test]
    public async Task Test_EndOfStream()
    {
        var buffer = new byte[16];
        using var ms = new MemoryStream(buffer);

        var source = await ChunkedMemoryStream.Create(ms, chunkSize: 16);
        await using var chunked = new ChunkedStream<ChunkedMemoryStream>(source);

        var bytesRead = await chunked.ReadAsync(buffer);
        await Assert.That(bytesRead).IsEqualTo(16);

        bytesRead = await chunked.ReadAsync(buffer);
        await Assert.That(bytesRead).IsZero().Because("Reached end of stream");
    }
    
    private class ChunkedMemoryStream : IChunkedStreamSource
    {
        private readonly (ulong Offset, byte[] Data)[] _chunks;

        private ChunkedMemoryStream((ulong Offset, byte[] Data)[] chunks, Size size)
        {
            _chunks = chunks;
            Size = size;
        }

        public static async ValueTask<ChunkedMemoryStream> Create(MemoryStream ms, int chunkSize)
        {
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

                ms.Position = offset;

                var buffer = new byte[size];
                await ms.ReadExactlyAsync(buffer);

                chunks.Add(((ulong)offset, buffer));
                offset += size;
            }

            return new ChunkedMemoryStream(chunks.ToArray(), Size.FromLong(ms.Length));
        }

        public Size Size { get; }
        public ulong ChunkCount => (ulong)_chunks.Length;
        public ulong GetOffset(ulong chunkIndex) => _chunks[chunkIndex].Offset;
        public int GetChunkSize(ulong chunkIndex) => _chunks[chunkIndex].Data.Length;

        public void ReadChunk(Span<byte> buffer, ulong chunkIndex)
        {
            Debug.Assert(buffer.Length == _chunks[chunkIndex].Data.Length);
            _chunks[chunkIndex].Data.CopyTo(buffer);
        }

        public Task ReadChunkAsync(Memory<byte> buffer, ulong chunkIndex, CancellationToken token = default)
        {
            Debug.Assert(buffer.Length == _chunks[chunkIndex].Data.Length);
            _chunks[chunkIndex].Data.CopyTo(buffer.Span);
            return Task.CompletedTask;
        }
    }
}
