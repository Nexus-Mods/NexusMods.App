using Microsoft.Extensions.DependencyInjection;
using NexusMods.FileExtractor.Extractors;

namespace NexusMods.FileExtractor;

public static class Services
{
    public static IServiceCollection AddFileExtractors(this IServiceCollection coll)
    {
        coll.AddSingleton<FileExtractor>();
        coll.AddSingleton<IExtractor, SevenZipExtractor>();
        return coll;
    }
    
}