using NexusMods.Common;
using NexusMods.FileExtractor.Extractors;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;

namespace NexusMods.Games.FOMOD.Tests.Mocks;

public class MockExtractor : IExtractor
{
    public FileType[] SupportedSignatures => new[] { FileType.ZIP };

    public Extension[] SupportedExtensions => new[] { (Extension)".zip" };

    public Priority DeterminePriority(IEnumerable<FileType> signatures)
    {
        return Priority.Highest;
    }

    public Task ExtractAllAsync(IStreamFactory source, AbsolutePath destination, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<IDictionary<RelativePath, T>> ForEachEntryAsync<T>(IStreamFactory source, Func<RelativePath, IStreamFactory, ValueTask<T>> func, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
