using System.IO.Hashing;
using System.Text;
using FluentAssertions;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging.Abstractions;
using NexusMods.FileExtractor.Extractors;
using NexusMods.Paths;
using NexusMods.Sdk.IO;

namespace NexusMods.FileExtractor.Tests;

public class ManagedZipExtractorTests
{
    [Theory]
    [InlineData("unicode-names.zip", "こんにちわ.txt")]
    [InlineData("unicode-extra-data.zip", "こんにちわ.txt")]
    public async Task Test(string archiveName, string expectedFileName)
    {
        // NOTE(erri120): The archive "unicode-extra-data" was created using the CreateZipFile() method
        // and then manually binary patched to remove the unicode flag.

        var archivePath = FileSystem.Shared.GetKnownPath(KnownPath.CurrentDirectory).Combine("Resources").Combine(archiveName);
        archivePath.FileExists.Should().BeTrue();

        var extractor = new ManagedZipExtractor(NullLogger<ManagedZipExtractor>.Instance);
        using var temporaryFileManager = new TemporaryFileManager(FileSystem.Shared);

        await using var destinationPath = temporaryFileManager.CreateFolder();
        await extractor.ExtractAllAsync(new NativeFileStreamFactory(archivePath), destinationPath);

        var files = destinationPath.Path.EnumerateFiles().ToArray();
        files.Should().ContainSingle().Which.Name.Should().Be(expectedFileName);
    }

    private static async Task CreateZipFile()
    {
        const string fileName = "こんにちわ.txt";
        var nameBytes = Encoding.UTF8.GetBytes(fileName);

        using var zipStream = new MemoryStream();
        await using (var outputStream = new ZipOutputStream(zipStream))
        {
            outputStream.IsStreamOwner = false;

            var fakeName = string.Create(length: nameBytes.Length, state: false, action: (span, _) => span.Fill('a'));
            var entry = new ZipEntry(fakeName)
            {
                ExtraData = CreateExtraData(nameBytes, fakeName),
            };

            var data = Encoding.UTF8.GetBytes(fileName);
            entry.Size = data.Length;

            await outputStream.PutNextEntryAsync(entry);
            await outputStream.WriteAsync(data);
            await outputStream.CloseEntryAsync(CancellationToken.None);
        }

        zipStream.Position = 0;

        await using (var fs = FileSystem.Shared.CreateFile(FileSystem.Shared.GetKnownPath(KnownPath.CurrentDirectory).Combine("b.zip")))
        {
            await zipStream.CopyToAsync(fs);
        }
    }

    private static byte[] CreateExtraData(byte[] nameBytes, string fakeName)
    {
        var fakeNameBytes = Encoding.UTF8.GetBytes(fakeName);
        var hash = Crc32.Hash(fakeNameBytes);

        var index = 0;
        var headerLength = sizeof(ushort) * 2;
        var dataLength = sizeof(byte) + sizeof(uint) + nameBytes.Length;
        var result = new byte[headerLength + dataLength];

        BitConverter.GetBytes((ushort)0x7075).AsSpan().CopyTo(result.AsSpan(start: index, length: sizeof(ushort)));
        index += sizeof(ushort);

        BitConverter.GetBytes((ushort)dataLength).AsSpan().CopyTo(result.AsSpan(start: index, length: sizeof(ushort)));
        index += sizeof(ushort);

        result[index++] = 1; // version

        hash.AsSpan().CopyTo(result.AsSpan(start: index));
        index += hash.Length;

        nameBytes.AsSpan().CopyTo(result.AsSpan(start: index));
        return result;
    }
}
