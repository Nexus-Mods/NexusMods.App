using System.IO.Compression;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Paths;
using NexusMods.Sdk.IO;

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
            var fixedPath = PathsHelper.FixPath(entry.FullName);
            var relativePath = RelativePath.FromUnsanitizedInput(fixedPath);
            var absolutePath = destination.Combine(relativePath);

            if (IsDirectoryEntry(entry))
            {
                if (!absolutePath.DirectoryExists()) absolutePath.CreateDirectory();
                continue;
            }

            var parent = absolutePath.Parent;
            if (!parent.DirectoryExists()) parent.CreateDirectory();

            await using var outputStream = absolutePath.Open(mode: FileMode.Create, access: FileAccess.ReadWrite, share: FileShare.Read);
            outputStream.SetLength(entry.Length);

            await using var inputStream = entry.Open();
            await inputStream.CopyToAsync(outputStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsDirectoryEntry(ZipArchiveEntry entry) => entry.Name.Length == 0;
    private static bool IsFileEntry(ZipArchiveEntry entry) => entry.Name.Length != 0;

    public Priority DeterminePriority(IEnumerable<FileType> signatures)
    {
        foreach (var signature in signatures)
        {
            if (signature == FileType.ZIP) return Priority.Highest;
        }

        return Priority.None;
    }
}
