using NexusMods.Common;
using NexusMods.FileExtractor.Extractors;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusMods.FOMOD.Tests;

public class MockExtractor : IExtractor
{
    public FileType[] SupportedSignatures => new FileType[] { FileType.ZIP };

    public Extension[] SupportedExtensions => new Extension[] { (Extension)".zip" };

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
