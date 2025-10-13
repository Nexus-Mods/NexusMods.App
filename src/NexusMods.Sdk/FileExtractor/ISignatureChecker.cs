using NexusMods.Abstractions.FileExtractor;

namespace NexusMods.Sdk.FileExtractor;

/// <summary>
/// A class that can look at the first few bytes of a file and determine what type of file it is
/// </summary>
public interface ISignatureChecker
{
    public ValueTask<bool> MatchesAnyAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks at the first few bytes of a stream and returns the file types that match. Note: this will only
    /// consider the filetypes that were passed into the factory.
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public ValueTask<IReadOnlyList<FileType>> MatchesAsync(Stream stream);
}
