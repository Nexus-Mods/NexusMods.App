using Microsoft.Extensions.Logging;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor.Extractors;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Interfaces;
using NexusMods.Interfaces.Streams;
using NexusMods.Paths;

namespace NexusMods.FileExtractor;

public class FileExtractor
{
    public SignatureChecker ArchiveSigs { get; }
    public HashSet<Extension> ExtractableExtensions { get; }

    private readonly ILogger<FileExtractor> _logger;
    private readonly TemporaryFileManager _manager;
    private readonly IEnumerable<IExtractor> _extractors;

    public FileExtractor(ILogger<FileExtractor> logger, TemporaryFileManager manager, IEnumerable<IExtractor> extractors)
    {
        _logger = logger;
        _manager = manager;
        _extractors = extractors.ToArray();

        ArchiveSigs = new SignatureChecker(_extractors.SelectMany(e => e.SupportedSignatures).Distinct().ToArray());
        ExtractableExtensions = _extractors.SelectMany(e => e.SupportedExtensions).ToHashSet();

    }

    private async ValueTask<IExtractor[]> FindExtractors(IStreamFactory sFn)
    {
        await using var archive = await sFn.GetStream();
        var sig = await ArchiveSigs.MatchesAsync(archive);

        return _extractors.Select(e => (Extractor: e, Result: e.DeterminePriority(sig)))
            .Where(e => e.Result != Priority.None)
            .OrderBy(e => e.Result)
            .Select(e => e.Extractor)
            .ToArray();
    }

    public async Task ExtractAll(AbsolutePath path, AbsolutePath dest, CancellationToken token)
    {
        await ExtractAll(new NativeFileStreamFactory(path), dest, token);
    }

    public async Task ExtractAll(IStreamFactory sFn, AbsolutePath dest, CancellationToken token)
    {
        var extractors = await FindExtractors(sFn);
        foreach (var extractor in extractors)
        {
            try
            {
                await extractor.ExtractAll(sFn, dest, token);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "While extracting via {Extractor}", extractor);
            }
        }
        
        throw new FileExtractionException($"No Extractors found for file {sFn.Name}");
    }

    public async Task<IDictionary<RelativePath, T>> ForEachEntry<T>(IStreamFactory sFn,
        Func<RelativePath, IStreamFactory, ValueTask<T>> func, CancellationToken token = default)
    {

        var extractors = await FindExtractors(sFn);

        foreach (var extractor in extractors)
        {
            try
            {
                return await extractor.ForEachEntry(sFn, func, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "While extracting via {Extractor}", extractor);
            }
        }

        throw new FileExtractionException("No Extractors found for file");
    }
    
    public async Task<IDictionary<RelativePath, T>> ForEachEntry<T>(AbsolutePath file,
        Func<RelativePath, IStreamFactory, ValueTask<T>> func, CancellationToken token = default)
    {
        return await ForEachEntry(new NativeFileStreamFactory(file), func, token);
    }

    public async Task<bool> CanExtract(AbsolutePath sFn)
    {
        return await CanExtract(new NativeFileStreamFactory(sFn));
    }

    public async Task<bool> CanExtract(IStreamFactory sFn)
    {
        await using var stream = await sFn.GetStream();
        return (await ArchiveSigs.MatchesAsync(stream)).Any();
    }
}