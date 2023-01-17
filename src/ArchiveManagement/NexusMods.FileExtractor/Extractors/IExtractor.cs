﻿using NexusMods.Common;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Interfaces;
using NexusMods.Paths;

namespace NexusMods.FileExtractor.Extractors;

public interface IExtractor
{
    /// <summary>
    /// Extracts maps `func` over every entry in an archive. The archive need not be extracted to disk if the
    /// archive can be decompressed on-the-fly in memoyr
    /// </summary>
    /// <param name="sFn">Source IStreamFactory</param>
    /// <param name="func">Function to apply to each entry in the archive</param>
    /// <param name="token">Cancellation token for the process</param>
    /// <typeparam name="T">Return type</typeparam>
    /// <returns>A Dictionary of RelativePath -> Return value from `func`</returns>
    public Task<IDictionary<RelativePath, T>> ForEachEntry<T>(IStreamFactory sFn,
        Func<RelativePath, IStreamFactory, ValueTask<T>> func, CancellationToken token = default);

    /// <summary>
    /// Unconditionally extract all files from `sFn` to a specific folder
    /// </summary>
    /// <param name="sFn"></param>
    /// <param name="destination"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task ExtractAll(IStreamFactory sFn, AbsolutePath destination, CancellationToken token);


    /// <summary>
    /// Given a FileType return the priority this extractor requests. Higher priority extractors will
    /// be tried first
    /// </summary>
    /// <param name="signatures"></param>
    /// <returns></returns>
    public Priority DeterminePriority(IEnumerable<FileType> signatures);

    /// <summary>
    /// A list of all the file type signatures supported by this extractor
    /// </summary>
    public IEnumerable<FileType> SupportedSignatures { get; }
    
    public IEnumerable<Extension> SupportedExtensions { get; }
}