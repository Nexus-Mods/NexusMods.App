using System.Diagnostics;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Paths;
using NexusMods.Sdk.IO;

namespace NexusMods.FileExtractor.Extractors;

internal class ManagedZipExtractor : IExtractor
{
    public FileType[] SupportedSignatures { get; } = [FileType.ZIP];
    public Extension[] SupportedExtensions { get; } = [new(".zip")];

    private readonly ILogger _logger;

    public ManagedZipExtractor(ILogger<ManagedZipExtractor> logger)
    {
        _logger = logger;
    }

    private static readonly StringCodec _stringCodec = StringCodec.Default;
    public async Task ExtractAllAsync(IStreamFactory source, AbsolutePath destination, CancellationToken cancellationToken = default)
    {
        await using var stream = await source.GetStreamAsync().ConfigureAwait(false);

        using var zipFile = new ZipFile(stream, leaveOpen: false, stringCodec: _stringCodec);
        for (var i = 0; i < zipFile.Count; i++)
        {
            var zipEntry = zipFile[i];

            var originalName = GetName(zipEntry);
            var fixedPath = PathsHelper.FixPath(originalName);
            var relativePath = RelativePath.FromUnsanitizedInput(fixedPath);
            var absolutePath = destination.Combine(relativePath);

            if (zipEntry.IsDirectory)
            {
                if (!absolutePath.DirectoryExists()) absolutePath.CreateDirectory();
                continue;
            }

            var parent = absolutePath.Parent;
            if (!parent.DirectoryExists()) parent.CreateDirectory();

            await using var outputStream = absolutePath.Open(mode: FileMode.Create, access: FileAccess.ReadWrite, share: FileShare.Read);
            outputStream.SetLength(zipEntry.Size);

            await using var inputStream = zipFile.GetInputStream(zipEntry);
            await inputStream.CopyToAsync(outputStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    private string GetName(ZipEntry zipEntry)
    {
        // NOTE(erri120): A ZIP entry name by default is encoded using CP436. If the general purpose bit flag 11
        // is set, then the name is instead encoded using UTF8. The SharpZipLib library handles that for us, and `zipEntry.Name`
        // will be a proper UTF8 decoded string if the flag was set.
        // However, the SharpZipLib library doesn't handle the 0x7076 extra data field which can contain the
        // UTF8 encoded path.

        var originalName = zipEntry.Name;
        if (zipEntry.HasFlag(GeneralBitFlags.UnicodeText)) return originalName;
        if (!TryGetUnicodeName(originalName, zipEntry.ExtraData, out var unicodeName, out var hashesMatch)) return originalName;

        if (!hashesMatch)
        {
            // NOTE(erri120): The CRC hash in the extra data field doesn't match with the computed hash of the original name
            // if the original name was encoded using an encoding different from the default encoding. It's not an issue,
            // it just means that now know that the original name was decoded with the wrong encoding.
            _logger.LogWarning("CRC hashes didn't match for encoded name in ZIP entry: Original name={OriginalName}, Unicode name={UnicodeName}", originalName, unicodeName);
        }

        return unicodeName;
    }

    private const ushort UnicodePathHeaderId = 0x7075;
    private static bool TryGetUnicodeName(ReadOnlySpan<char> originalName, ReadOnlySpan<byte> extraData, out string unicodeName, out bool hashesMatch)
    {
        unicodeName = string.Empty;
        hashesMatch = false;
        if (extraData.Length < ExtraDataHeader.Size) return false;

        // Layout:
        // |-- Header 1..N
        // |-- ushort HeaderId
        // |-- ushort DataLength
        // |-- byte[DataLength] Data
        // continue until everything is read

        var index = 0;
        while (index + ExtraDataHeader.Size < extraData.Length)
        {
            var span = extraData.Slice(start: index, length: ExtraDataHeader.Size);
            var header = MemoryMarshal.Read<ExtraDataHeader>(span);

            if (header.HeaderId != UnicodePathHeaderId)
            {
                index += ExtraDataHeader.Size + header.DataSize;
                continue;
            }

            var dataSpan = extraData.Slice(start: index + ExtraDataHeader.Size, length: header.DataSize);
            (unicodeName, hashesMatch) = DecodeUnicodeName(originalName, dataSpan);
            return true;
        }

        return false;
    }

    private static (string UnicodeName, bool HashesMatch) DecodeUnicodeName(ReadOnlySpan<char> originalName, ReadOnlySpan<byte> span)
    {
        // Layout
        // byte version (should always be 1)
        // uint crcHash (hash of the original name)
        // byte[] utf8Name (remaining data is the UTF8 encoded name)

        var index = 0;

        var version = span[index++];
        Debug.Assert(version == 1, $"unknown version `{version}`");

        var actualHash = BitConverter.ToUInt32(span.Slice(start: index, length: sizeof(uint)));
        index += sizeof(uint);

        var expectedHash = GetCrcHash(originalName);
        var hashesMatch = actualHash == expectedHash;

        var nameSlice = span.Slice(start: index);
        var unicodeName = Encoding.UTF8.GetString(nameSlice);
        return (unicodeName, hashesMatch);
    }

    private static uint GetCrcHash(ReadOnlySpan<char> originalName)
    {
        var byteCount = _stringCodec.LegacyEncoding.GetByteCount(originalName);
        var bytes = byteCount < 1024 ? stackalloc byte[byteCount] : GC.AllocateUninitializedArray<byte>(byteCount);
        var actualCount = _stringCodec.LegacyEncoding.GetBytes(originalName, bytes);
        Debug.Assert(actualCount == byteCount);

        Span<byte> hashBytes = stackalloc byte[sizeof(uint)];
        var hashBytesCount = Crc32.Hash(bytes.Slice(start: 0, length: actualCount), hashBytes);
        Debug.Assert(hashBytesCount == sizeof(uint));

        return BitConverter.ToUInt32(hashBytes);
    }

    [StructLayout(LayoutKind.Explicit)]
    private readonly struct ExtraDataHeader
    {
        public const int Size = 4;

        [FieldOffset(0)] public readonly ushort HeaderId;
        [FieldOffset(2)] public readonly ushort DataSize;

        static ExtraDataHeader()
        {
            unsafe
            {
                var size = sizeof(ExtraDataHeader);
                if (size != Size) throw new Exception($"Expected size of be {Size} but found {size} for {typeof(ExtraDataHeader)}");
            }
        }
    }

    public Priority DeterminePriority(IEnumerable<FileType> signatures)
    {
        foreach (var signature in signatures)
        {
            if (signature == FileType.ZIP) return Priority.Highest;
        }

        return Priority.None;
    }
}
