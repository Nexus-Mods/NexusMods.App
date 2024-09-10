using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia;
using BitFaster.Caching.Lfu;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using SkiaSharp;

namespace NexusMods.App.UI;

public sealed class ImageStore : IImageStore, IDisposable, IAsyncDisposable
{
    private const byte BinaryRevision = 1;
    private const int HeaderSize = sizeof(byte) + sizeof(uint) + 15;
    private static readonly int MetadataSize = Marshal.SizeOf<ImageMetadata>();

    private static readonly Lazy<IImageStore> LazyInstance = new(
        // TODO: other path?
        valueFactory: () => new ImageStore(FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)),
        mode: LazyThreadSafetyMode.ExecutionAndPublication
    );

    public static IImageStore Instance => LazyInstance.Value;

    private Stream _metadataStream;
    private Stream _dataStream;
    private ConcurrentDictionary<EntityId, ImageMetadata> _metadataTable;
    private ConcurrentLfu<EntityId, Bitmap> _bitmapCache;

    private byte[] _headerData = [];

    private readonly SemaphoreSlim _streamSemaphore = new(initialCount: 1, maxCount: 1);

    private readonly object _dataStartLock = new();
    private ulong _currentDataStart;

    internal ImageStore(AbsolutePath directory)
    {
        _bitmapCache = new ConcurrentLfu<EntityId, Bitmap>(capacity: 50);

        _metadataStream = new FileStream(directory.Combine("image-store-metadata.bin").ToNativeSeparators(OSInformation.Shared), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        _dataStream = new FileStream(directory.Combine("image-store-data.bin").ToNativeSeparators(OSInformation.Shared), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

        var init = InitMetadataTable(_metadataStream);
        if (init is not null)
        {
            _metadataTable = init;
        }
        else
        {
            _metadataStream.Position = 0;
            _metadataStream.SetLength(0);
            _dataStream.Position = 0;
            _dataStream.SetLength(0);

            _metadataTable = new ConcurrentDictionary<EntityId, ImageMetadata>();
        }
    }

    public async ValueTask Store(EntityId id, Bitmap bitmap)
    {
        ImageMetadata metadata;

        lock (_dataStartLock)
        {
            metadata = ToMetadata(id, bitmap, dataStart: _currentDataStart);
            _currentDataStart += metadata.DataLength;
        }

        _metadataTable[id] = metadata;
        var numEntries = _metadataTable.Count;

        var bytes = ArrayPool<byte>.Shared.Rent(minimumLength: (int)metadata.DataLength);
        try
        {
            GetBitmapBytes(metadata, bitmap, bytes);

            using var _ = _streamSemaphore.WaitDisposable();
            _dataStream.Position = (long)metadata.DataStart;
            await _dataStream.WriteAsync(bytes.AsMemory(start: 0, length: (int)metadata.DataLength));

            metadata.Write(bytes);
            await _metadataStream.WriteAsync(bytes.AsMemory(start: 0, length: MetadataSize));

            await UpdateMetadataCount(newCount: (uint)numEntries);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }
    }

    private async ValueTask UpdateMetadataCount(uint newCount)
    {
        var streamPosition = _metadataStream.Position;
        _metadataStream.Position = sizeof(byte); // offset: byte revision

        var buf = GC.AllocateUninitializedArray<byte>(sizeof(uint));
        BinaryPrimitives.WriteUInt32LittleEndian(buf, newCount);

        await _metadataStream.WriteAsync(buf);

        _metadataStream.Position = streamPosition;
    }

    private static void GetBitmapBytes(ImageMetadata metadata, Bitmap bitmap, byte[] bytes)
    {
        unsafe
        {
            fixed (byte* b = bytes)
            {
                var ptr = new IntPtr(b);
                bitmap.CopyPixels(
                    sourceRect: new PixelRect(metadata.PixelSize),
                    buffer: ptr,
                    bufferSize: (int)metadata.DataLength,
                    stride: metadata.Stride
                );
            }
        }
    }

    public async ValueTask<Bitmap?> Retrieve(EntityId id)
    {
        if (!_metadataTable.TryGetValue(id, out var metadata)) return null;

        return await _bitmapCache.GetOrAddAsync(id, static async (id, state) =>
        {
            var (self, metadata) = state;

            var bytes = ArrayPool<byte>.Shared.Rent(minimumLength: (int)metadata.DataLength);
            try
            {
                using var _ = self._streamSemaphore.WaitDisposable();
                self._dataStream.Position = (long)metadata.DataStart;

                var memory = bytes.AsMemory(start: 0, length: (int)metadata.DataLength);
                var numBytes = await self._dataStream.ReadAsync(memory);
                Debug.Assert((uint)numBytes == metadata.DataLength);

                return ToBitmap(metadata, bytes);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }, (this, metadata));
    }

    private static Bitmap ToBitmap(ImageMetadata metadata, byte[] bytes)
    {
        unsafe
        {
            fixed (byte* b = bytes)
            {
                var ptr = new IntPtr(b);
                var bitmap = new Bitmap(
                    format: metadata.PixelFormat,
                    alphaFormat: AlphaFormat.Opaque,
                    data: ptr,
                    size: metadata.PixelSize,
                    dpi: new Vector(96, 96),
                    stride: metadata.Stride
                );

                return bitmap;
            }
        }
    }

    private ConcurrentDictionary<EntityId, ImageMetadata>? InitMetadataTable(Stream stream)
    {
        if (stream.Length < HeaderSize) return null;

        // Format:
        // byte - BinaryRevision
        // uint - number of entries
        // 15 bytes padding
        // entries

        _headerData = new byte[HeaderSize];
        _ = stream.Read(_headerData);

        if (_headerData[0] != BinaryRevision) return null;

        Span<byte> entryBuffer = stackalloc byte[MetadataSize];
        var numEntries = BinaryPrimitives.ReadUInt32LittleEndian(_headerData.AsSpan()[1..sizeof(uint)]);

        if (stream.Length != MetadataSize * numEntries) return null;

        var entries = GC.AllocateUninitializedArray<KeyValuePair<EntityId, ImageMetadata>>(length: (int)numEntries);

        for (var i = 0; i < numEntries; i++)
        {
            var numBytes = stream.Read(entryBuffer);
            Debug.Assert(numBytes == entryBuffer.Length);

            var value = ImageMetadata.Read(entryBuffer);
            entries[i] = new KeyValuePair<EntityId, ImageMetadata>(value.Id, value);
        }

        return new ConcurrentDictionary<EntityId, ImageMetadata>(entries);
    }

    private bool _isDisposed;
    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        _metadataStream.Dispose();
        _dataStream.Dispose();
        _streamSemaphore.Dispose();

        _metadataStream = null!;
        _dataStream = null!;
        _metadataTable = null!;
        _bitmapCache = null!;
        _isDisposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        await _metadataStream.DisposeAsync();
        await _dataStream.DisposeAsync();

        _metadataStream = null!;
        _dataStream = null!;
        _metadataTable = null!;
        _bitmapCache = null!;
        _isDisposed = true;
    }

    private static ImageMetadata ToMetadata(EntityId id, Bitmap bitmap, ulong dataStart)
    {
        var width = (uint)bitmap.PixelSize.Width;
        var height = (uint)bitmap.PixelSize.Height;

        if (!bitmap.Format.HasValue) throw new NotSupportedException();
        var format = bitmap.Format.Value;

        // NOTE(erri120): small hack, Avalonia PixelFormat struct is sealed, and we can't really do anything with it.
        // Instead, we'll just convert to SkColorType using the method provided by Avalonia.
        var skColorType = format.ToSkColorType();
        skColorType.ToPixelFormat();

        var metadata = new ImageMetadata(id, imageWidth: width, imageHeight: height, skColorType: skColorType, dataStart: dataStart);
        return metadata;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct ImageMetadata
    {
        public readonly EntityId Id;
        public readonly uint ImageWidth;
        public readonly uint ImageHeight;
        public readonly SKColorType SkColorType;
        public readonly ulong DataStart;

        public PixelSize PixelSize => new((int)ImageWidth, (int)ImageHeight);
        public PixelFormat PixelFormat => SkColorType.ToPixelFormat();
        public int Stride => (int)ImageWidth * PixelFormat.BitsPerPixel;

        // NOTE(erri120): Going from bits to bytes requires dividing by 8, aka bit shift by 3
        public ulong DataLength => (ImageWidth * ImageHeight * (uint)PixelFormat.BitsPerPixel) >> 3;

        public ImageMetadata(EntityId id, uint imageWidth, uint imageHeight, SKColorType skColorType, ulong dataStart)
        {
            Id = id;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
            SkColorType = skColorType;
            DataStart = dataStart;

            Guard(skColorType);
        }

        public static ImageMetadata Read(ReadOnlySpan<byte> bytes)
        {
            Debug.Assert(bytes.Length == Marshal.SizeOf<ImageMetadata>());

            unsafe
            {
                fixed (byte* b = bytes)
                {
                    return Unsafe.Read<ImageMetadata>(b);
                }
            }
        }

        public void Write(Span<byte> bytes)
        {
            Debug.Assert(bytes.Length == Marshal.SizeOf<ImageMetadata>());

            unsafe
            {
                fixed (void* b = bytes)
                {
                    Unsafe.Write(b, this);
                }
            }
        }

        private static void Guard(SKColorType skColorType)
        {
            // NOTE(erri120): safe-guard, this should never be hit in the real world, only odd formats like greyscale images
            // would trigger this.
            if (skColorType.ToPixelFormat().BitsPerPixel % 8 == 0) return;
            ThrowHelper(skColorType);
        }

        [DoesNotReturn]
        private static void ThrowHelper(SKColorType skColorType)
        {
            throw new NotSupportedException($"BitsPerPixel of `{skColorType.ToPixelFormat()}` is `{skColorType.ToPixelFormat().BitsPerPixel}` and not divisible by 8!");
        }
    }
}

