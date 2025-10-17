using NexusMods.Abstractions.FileExtractor;

namespace NexusMods.Sdk.FileExtractor;

/// <summary>
/// A factory for creating signature checkers
/// </summary>
public interface ISignatureCheckerFactory
{
    /// <summary>
    /// Creates a signature checker that can check for the given file types.
    /// </summary>
    /// <param name="fileTypes"></param>
    /// <returns></returns>
    public ISignatureChecker Create(params FileType[] fileTypes);
}
