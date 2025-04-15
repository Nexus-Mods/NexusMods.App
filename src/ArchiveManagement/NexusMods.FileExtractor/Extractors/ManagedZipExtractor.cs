using System.IO.Compression;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.IO;
using NexusMods.Paths;

namespace NexusMods.FileExtractor.Extractors;

internal class ManagedZipExtractor : IExtractor
{
    public FileType[] SupportedSignatures { get; } = [FileType.ZIP];
    public Extension[] SupportedExtensions { get; } = [new(".zip")];

    public async Task ExtractAllAsync(IStreamFactory source, AbsolutePath destination, CancellationToken cancellationToken = default)
    {
        await using var stream = await source.GetStreamAsync().ConfigureAwait(false);

        using var archive = new ZipArchive(stream, mode: ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null);
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\')) continue;

            var fileName = RelativePath.FromUnsanitizedInput(PathsHelper.FixFileName(entry.FullName));
            var outputPath = destination.Combine(fileName);

            var parent = outputPath.Parent;
            if (!parent.DirectoryExists()) parent.CreateDirectory();

            await using var outputStream = outputPath.Open(mode: FileMode.Create, access: FileAccess.ReadWrite, share: FileShare.Read);
            outputStream.SetLength(entry.Length);

            await using var inputStream = entry.Open();

            await inputStream.CopyToAsync(outputStream, cancellationToken: cancellationToken);
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
