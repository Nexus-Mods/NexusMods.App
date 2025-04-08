using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.FileExtractor.Extractors;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;

namespace NexusMods.FileExtractor;

/// <summary>
/// Utility used for extracting files.
/// This utility is usually created via DI container.
/// See <see cref="Services.AddFileExtractors"/>; and then ask the DI container nicely to give you an instance of this :)
/// </summary>
public class FileExtractor : IFileExtractor
{
    /// <summary>
    /// Used to check for file types by scanning file header signatures.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public SignatureChecker SignatureChecker { get; }

    private readonly ILogger<FileExtractor> _logger;
    private readonly IEnumerable<IExtractor> _extractors;

    /// <summary>
    /// Creates a new instance of a File Extractor
    /// </summary>
    /// <param name="logger">Logger used to report back.</param>
    /// <param name="extractors">Extractors used to unpack files to disk.</param>
    /// <remarks>
    ///    This method is usually called via DI container.
    ///    See <see cref="Services.AddFileExtractors"/>; and then ask the DI container nicely to give you an instance of this.
    /// </remarks>
    public FileExtractor(ILogger<FileExtractor> logger, IEnumerable<IExtractor> extractors)
    {
        _logger = logger;
        _extractors = extractors.ToArray();
        SignatureChecker = new SignatureChecker(_extractors.SelectMany(e => e.SupportedSignatures).Distinct().ToArray());
    }

    /// <summary>
    /// Extracts all files to disk in an asynchronous fashion.
    /// </summary>
    /// <param name="path">The path to the source file.</param>
    /// <param name="dest">The folder where the file is to be extracted.</param>
    /// <param name="token">Used for cancellation of the operation.</param>
    public async Task ExtractAllAsync(AbsolutePath path, AbsolutePath dest, CancellationToken token = default)
    {
        await ExtractAllAsync(new NativeFileStreamFactory(path), dest, token);
    }

    /// <summary>
    /// Extracts all files to disk in an asynchronous fashion.
    /// </summary>
    /// <param name="sFn">The source stream.</param>
    /// <param name="dest">Destination file path.</param>
    /// <param name="token">Used for cancellation of the operation.</param>
    /// <exception cref="FileExtractionException"></exception>
    public async Task ExtractAllAsync(IStreamFactory sFn, AbsolutePath dest, CancellationToken token = default)
    {
        var extractors = await FindExtractors(sFn);
        foreach (var extractor in extractors)
        {
            try
            {
                await extractor.ExtractAllAsync(sFn, dest, token);
                FixPaths(destinationPath: dest);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "While extracting via {Extractor}", extractor);
            }
        }

        throw new FileExtractionException($"No Extractors found for file {sFn.Name}");
    }

    /// <summary>
    /// Extracts and calls `func` over every entry in an archive.
    /// </summary>
    /// <param name="source">The source of the incoming stream</param>
    /// <param name="func">Function to apply to each entry in the archive</param>
    /// <param name="token">Cancellation token for the process</param>
    /// <typeparam name="T">Return type</typeparam>
    /// <returns>A Dictionary of RelativePath -> Return value from `func`</returns>
    /// <remarks>
    ///     Does not extract files to disk. If you need to save the data; copy it elsewhere.
    ///     The source data passed to func can be in-memory.
    /// </remarks>
    public async Task<IDictionary<RelativePath, T>> ForEachEntry<T>(IStreamFactory source,
        Func<RelativePath, IStreamFactory, ValueTask<T>> func, CancellationToken token = default)
    {
        var extractors = await FindExtractors(source);
        foreach (var extractor in extractors)
        {
            try
            {
                return await extractor.ForEachEntryAsync(source, func, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "While extracting via {Extractor}", extractor);
            }
        }

        throw new FileExtractionException("No Extractors found for file");
    }

    [SuppressMessage("ReSharper", "RedundantNameQualifier")]
    private void FixPaths(AbsolutePath destinationPath)
    {
        var directoryQueue = new Queue<string>();
        directoryQueue.Enqueue(destinationPath.ToNativeSeparators(OSInformation.Shared));

        while (directoryQueue.TryDequeue(out var directoryToCheck))
        {
            if (!System.IO.Directory.Exists(directoryToCheck)) continue;

            foreach (var file in System.IO.Directory.EnumerateFiles(directoryToCheck, searchPattern: "*", searchOption: SearchOption.TopDirectoryOnly))
            {
                var destination = FixPath(directoryToCheck, file);
                if (destination is null) continue;

                try
                {
                    _logger.LogWarning("Fixing path by moving file from `{From}` to `{To}`", file, destination);
                    System.IO.File.Move(file, destination);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to fix path by moving file from `{From}` to `{To}`", file, destination);
                }
            }

            foreach (var subDirectory in System.IO.Directory.EnumerateDirectories(directoryToCheck, searchPattern: "*", searchOption: SearchOption.TopDirectoryOnly))
            {
                var destination = FixPath(directoryToCheck, subDirectory);
                directoryQueue.Enqueue(destination ?? subDirectory);

                if (destination is null) continue;

                try
                {
                    _logger.LogWarning("Fixing path by moving directory from `{From}` to `{To}`", subDirectory, destination);
                    System.IO.Directory.Move(subDirectory, destination);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to fix path by moving directory from `{From}` to `{To}`", subDirectory, destination);
                }
            }
        }

        return;
        string? FixPath(string directoryToCheck, ReadOnlySpan<char> input)
        {
            var span = System.IO.Path.GetFileName(input);
            var trimmed = span.Trim();
            if (span.Equals(trimmed, StringComparison.Ordinal)) return null;

            var destination = System.IO.Path.Combine(directoryToCheck, trimmed.ToString());
            return destination;
        }
    }

    /// <summary>
    /// Extracts and calls `func` over every entry in an archive.
    /// </summary>
    /// <param name="file">Path to the archive to perform the operation on</param>
    /// <param name="func">Function to apply to each entry in the archive</param>
    /// <param name="token">Cancellation token for the process</param>
    /// <typeparam name="T">Return type</typeparam>
    /// <returns>A Dictionary of RelativePath -> Return value from `func`</returns>
    /// <remarks>
    ///     Does not extract files to disk. If you need to save the data; copy it elsewhere.
    ///     The source data passed to func can be in-memory.
    /// </remarks>
    public async Task<IDictionary<RelativePath, T>> ForEachEntry<T>(AbsolutePath file,
        Func<RelativePath, IStreamFactory, ValueTask<T>> func, CancellationToken token = default)
    {
        return await ForEachEntry(new NativeFileStreamFactory(file), func, token);
    }

    /// <inheritdoc/>
    public ValueTask<bool> CanExtract(AbsolutePath path)
    {
        return CanExtract(new NativeFileStreamFactory(path));
    }

    /// <inheritdoc/>
    public async ValueTask<bool> CanExtract(IStreamFactory sFn)
    {
        return await CanExtract(await sFn.GetStreamAsync());
    }

    /// <inheritdoc/>
    public async ValueTask<bool> CanExtract(Stream stream)
    {
        return await SignatureChecker.MatchesAnyAsync(stream);
    }

    private async ValueTask<IExtractor[]> FindExtractors(IStreamFactory sFn)
    {
        await using var archive = await sFn.GetStreamAsync();
        var sig = await SignatureChecker.MatchesAsync(archive);

        return _extractors.Select(e => (Extractor: e, Result: e.DeterminePriority(sig)))
            .Where(e => e.Result != Priority.None)
            .OrderBy(e => e.Result)
            .Select(e => e.Extractor)
            .ToArray();
    }
}
