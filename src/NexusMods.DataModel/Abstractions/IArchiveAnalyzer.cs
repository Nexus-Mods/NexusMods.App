using NexusMods.DataModel.ArchiveContents;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Abstractions;

public interface IArchiveAnalyzer
{
    /// <summary>
    /// Analyzes a file (normally an archive). If the file is an archive it will be added to the archive manager.
    /// The analysis data will be cached and returned. 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<AnalyzedFile> AnalyzeFileAsync(AbsolutePath path, CancellationToken token = default);
    
    /// <summary>
    /// Gets the analysis data for the given hash. If the hash is not known, null will be returned.
    /// </summary>
    /// <param name="hash"></param>
    /// <returns></returns>
    public AnalyzedFile? GetAnalysisData(Hash hash);
}
