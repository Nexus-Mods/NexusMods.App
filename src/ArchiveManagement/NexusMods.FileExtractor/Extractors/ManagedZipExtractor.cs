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

        var directoryMapping = new Dictionary<string, RelativePath>(StringComparer.Ordinal);
        foreach (var directoryEntry in archive.Entries.Where(IsDirectoryEntry))
        {
            var relativeDirectoryPath = RelativePath.FromUnsanitizedInput(PathsHelper.FixDirectoryName(directoryEntry.FullName));
            directoryMapping.Add(directoryEntry.FullName, relativeDirectoryPath);

            var directoryPath = destination.Combine(relativeDirectoryPath);
            if (!directoryPath.DirectoryExists()) directoryPath.CreateDirectory();
        }

        foreach (var fileEntry in archive.Entries.Where(IsFileEntry))
        {
            var fileName = RelativePath.FromUnsanitizedInput(PathsHelper.FixFileName(fileEntry.Name));
            var directory = directoryMapping
                .Where(kv => fileEntry.FullName.StartsWith(kv.Key, StringComparison.Ordinal))
                .OrderByDescending(kv => kv.Key, StringComparer.Ordinal)
                .First();

            var relativePath = directory.Value.Join(fileName);
            var outputPath = destination.Combine(relativePath);

            await using var outputStream = outputPath.Open(mode: FileMode.Create, access: FileAccess.ReadWrite, share: FileShare.Read);
            outputStream.SetLength(fileEntry.Length);

            await using var inputStream = fileEntry.Open();

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
